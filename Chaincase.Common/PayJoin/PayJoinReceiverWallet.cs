using System;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.BIP78.Receiver;
using BTCPayServer.BIP78.Sender;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using NBitcoin;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Wallets;

namespace Chaincase.Common.PayJoin
{
    public class PayJoinProposalContext : PayjoinProposalContext
    {
        public PayJoinProposalContext(PSBT originalPSBT, PayjoinClientParameters payjoinClientParameters = null)
            : base(originalPSBT, payjoinClientParameters)
        {
        }

        // Chaincase doesn't track payment request Amount, so ignore it
        public override void SetPaymentRequest(PayjoinPaymentRequest paymentRequest)
        {
            PaymentRequest = paymentRequest;
            OriginalPaymentRequestOutput = OriginalPSBT.Outputs.Single(output =>
                output.ScriptPubKey == paymentRequest.Destination.ScriptPubKey);
        }
    }

    // bip78 spec: https://github.com/bitcoin/bips/blob/master/bip-0078.mediawiki
    public class PayJoinReceiverWallet<TContext> : PayjoinReceiverWallet<TContext>
        where TContext : PayjoinProposalContext
    {
        private readonly Network _network;
        private readonly ChaincaseWalletManager _walletManager;
        private readonly INotificationManager _notificationManager;
        private readonly int _privacyLevelThreshold;

        public PayJoinReceiverWallet(Network network, ChaincaseWalletManager walletManager, INotificationManager notificationManager, int privacyLevelThreshold = 1)
        {
            _network = network;
            _walletManager = walletManager;
            _notificationManager = notificationManager;
            _privacyLevelThreshold = privacyLevelThreshold;
        }

        // could be called HandleProposal or the like to reflect the spec
        public override async Task Initiate(TContext ctx)
        {
            var paymentRequest = await FindMatchingPaymentRequests(ctx);
            if (paymentRequest is null)
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    $"Could not match PSBT to a payment request");
            }

            ctx.SetPaymentRequest(paymentRequest);

            if (ctx.PayjoinParameters.Version != 1)
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.VersionUnsupported),
                    PayjoinReceiverHelper.GetMessage(PayjoinReceiverWellknownErrors.VersionUnsupported));
            }

            if (!ctx.OriginalPSBT.IsAllFinalized())
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    "The PSBT should be finalized");

            var sendersInputType = ctx.OriginalPSBT.GetInputsScriptPubKeyType();
            if (sendersInputType is null || !await SupportsType(sendersInputType.Value))
            {
                //this should never happen, unless the store owner changed the wallet mid way through an invoice
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.Unavailable),
                    "Our wallet does not support this wallet format");
            }

            if (ctx.OriginalPSBT.CheckSanity() is var errors && errors.Count != 0)
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    $"This PSBT is insane ({errors[0]})");
            }

            FeeRate originalFeeRate;
            if (!ctx.OriginalPSBT.TryGetEstimatedFeeRate(out originalFeeRate))
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    "You need to provide Witness UTXO information to the PSBT.");
            }

            // This is not a mandatory check.
            // Regardless, we don't want any implementation to leak global xpubs
            if (ctx.OriginalPSBT.GlobalXPubs.Any())
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    "GlobalXPubs should not be included in the PSBT");
            }

            if (ctx.OriginalPSBT.Outputs.Any(o => o.HDKeyPaths.Count != 0) ||
                ctx.OriginalPSBT.Inputs.Any(o => o.HDKeyPaths.Count != 0))
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    "Keypath information should not be included in the PSBT");
            }

            if (ctx.OriginalPSBT.Inputs.Any(o => !o.IsFinalized()))
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    "The PSBT Should be finalized");
            }

            var mempoolError = await IsMempoolEligible(ctx.OriginalPSBT);
            if (!string.IsNullOrEmpty(mempoolError))
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    $"Provided transaction isn't mempool eligible {mempoolError}");
            }

            var groupedOutputs = ctx.OriginalPSBT.Outputs.GroupBy(output => output.ScriptPubKey);
            var paymentOutputs =
                groupedOutputs.Where(outputs => outputs.Key == paymentRequest.Destination.ScriptPubKey);

            if (!paymentOutputs.Any())
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    "PSBT does not pay to the BIP21 destination");
            }

            if (ctx.PayjoinParameters.AdditionalFeeOutputIndex.HasValue && paymentOutputs.Any(outputs =>
                outputs.Any(output => output.Index == ctx.PayjoinParameters.AdditionalFeeOutputIndex)))
            {
                throw new PayjoinReceiverException(
                    PayjoinReceiverHelper.GetErrorCode(PayjoinReceiverWellknownErrors.OriginalPSBTRejected),
                    "AdditionalFeeOutputIndex specified index of payment output");
            }

            ctx = await ComputePayjoin(ctx);
        }

        public string GetPayjoinProposalResult(TContext ctx) =>
            ctx.PayjoinReceiverWalletProposal.PayjoinPSBT.ToBase64();


        //  "Interactive receivers are not required to validate [or broadcast]
        //  the original PSBT because they are not exposed to probing attacks."
        // — bip78 spec
        protected override Task BroadcastOriginalTransaction(TContext context, TimeSpan scheduledTime)
        {
            throw new NotImplementedException();
        }

        // this is where we ought to add our input & output
        // Instead of modifying the context we'll create a new one
        // pure functional code is much easier to reason with
        protected override Task ComputePayjoinModifications(TContext context)
        {
            throw new NotImplementedException();
        }

        // TODO pass in spendable utxos, rm km dep for functional reasoning
        // perhaps just return the psbt instead of this context
        protected Task<TContext> ComputePayjoin(TContext context, string password = "")
        {
            //from btcpayserver's payjoinEndpoint
            //var utxos = (await explorer.GetUTXOsAsync(derivationSchemeSettings.AccountDerivation))
            //        .GetUnspentUTXOs(false);
            //// In case we are paying ourselves, be need to make sure
            //// we can't take spent outpoints.
            //var prevOuts = ctx.OriginalTransaction.Inputs.Select(o => o.PrevOut).ToHashSet();
            //utxos = utxos.Where(u => !prevOuts.Contains(u.Outpoint)).ToArray();
            //Array.Sort(utxos, UTXODeterministicComparer.Instance);
            //foreach (var utxo in (await SelectUTXO(network, utxos, psbt.Inputs.Select(input => input.WitnessUtxo.Value.ToDecimal(MoneyUnit.BTC)), output.Value.ToDecimal(MoneyUnit.BTC),
            //    psbt.Outputs.Where(psbtOutput => psbtOutput.Index != output.Index).Select(psbtOutput => psbtOutput.Value.ToDecimal(MoneyUnit.BTC)))).selectedUTXO)
            //{
            //    selectedUTXOs.Add(utxo.Outpoint, utxo);
            //}
            //ctx.LockedUTXOs = selectedUTXOs.Select(u => u.Key).ToArray();
            //originalPaymentOutput = output;

            // Which coin to use

            var toUse = _walletManager.GetWallets()
                .Where(x => x.State == WalletState.Started && !x.KeyManager.IsWatchOnly && !x.KeyManager.IsHardwareWallet)
                .SelectMany(wallet => wallet.Coins.Select(coin => new { wallet.KeyManager, coin }))
                .Where(x => x.coin.AnonymitySet >= _privacyLevelThreshold && !x.coin.Unavailable)
                .OrderBy(x => x.coin.IsBanned)
                .ThenBy(x => x.coin.Confirmed)
                .ThenBy(x => x.coin.Height)
                .First();
            var psbt = context.OriginalPSBT;

            // Fees 
            context.OriginalPSBT.TryGetEstimatedFeeRate(out var originalFeeRate);
            var paymentTx = psbt.ExtractTransaction();
            foreach (var input in paymentTx.Inputs)
            {
                input.WitScript = WitScript.Empty;
            }
            // Get prv key for signature 
            var serverCoinKey = toUse.KeyManager.GetSecrets(password, toUse.coin.ScriptPubKey).First();
            var serverCoin = toUse.coin.GetCoin();

            paymentTx.Inputs.Add(serverCoin.Outpoint);
            var paymentOutput = paymentTx.Outputs.First();
            var inputSizeInVBytes = (int)Math.Ceiling(((3 * Constants.P2wpkhInputSizeInBytes) + Constants.P2pkhInputSizeInBytes) / 4m);
            // Get final value
            paymentOutput.Value += (Money)serverCoin.Amount - originalFeeRate.GetFee(inputSizeInVBytes);

            var payjoinPSBT = PSBT.FromTransaction(paymentTx, Network.Main);
            var serverCoinToSign = payjoinPSBT.Inputs.FindIndexedInput(serverCoin.Outpoint);
            serverCoinToSign.UpdateFromCoin(serverCoin);
            serverCoinToSign.Sign(serverCoinKey.PrivateKey);
            serverCoinToSign.FinalizeInput();

            _notificationManager.ScheduleNotification("PayJoin Received", "You've responded to a payment request with a CoinJoin style transaction ", 1);
            payjoinPSBT.ToHex();

            var computed = new PayjoinProposalContext(context.OriginalPSBT, context.PayjoinParameters);
            computed.PayjoinReceiverWalletProposal = new PayjoinReceiverWalletProposal
            {
                PayjoinPSBT = payjoinPSBT
            };

            return Task.FromResult((TContext)computed);
        }

        // We don't yet maintain payment requests with amounts like btcpay does
        // Wallet.json only maintains a list of pubkeys
        // "HdPubKeys": [
        //{
        //  "PubKey": "<redacted>",
        //  "FullKeyPath": "84'/0'/0'/1/0",
        //  "Label": "",
        //  "KeyState": 0
        //}
        // Why is this method plural if it expects to return one item?
        protected override Task<PayjoinPaymentRequest> FindMatchingPaymentRequests(TContext context)
        {
            // PayJoin Client deserves only an key iteration abstraction.
            // I don't like that it has to know about KeyState, and internal vs external
            // We can't ship new abstractions that rely on this KeyManager
            // The btc way of "paymentRequest" db makes more sense to me
            var km = _walletManager.CurrentWallet.KeyManager;
            var paymentRequestOutputs = km.GetKeys(KeyState.Locked, isInternal: false);
            var payjoinOutputScripts = context.OriginalPSBT.Outputs.Select(output => output.ScriptPubKey);
            var matching = payjoinOutputScripts.Where(pj => paymentRequestOutputs.Any(r => pj.Equals(r.P2wpkhScript))).Single();

            var paymentRequest = new PayjoinPaymentRequest
            {
                Destination = matching.GetDestinationAddress(_network)
                // Chaincase doesn't track payment request Amount
            };
            return Task.FromResult(paymentRequest);
        }

        // "Note that probing attacks are only a problem for
        // automated payment systems such as BTCPay Server. End-user wallets
        // with payjoin capabilities are not affected, as the attacker can't
        // create multiple invoices to force the receiver to expose their UTXOs."
        // — bip78 spec
        //
        // That's what InputsSeenBefore aims to solve, so we can ignore it.
        // Todo this also prevents reentrant payjoin, where a sender attempts to
        // use payjoin transaction as a new original transaction for a new payjoin.
        protected override Task<bool> InputsSeenBefore(PSBTInputList inputList)
        {
            throw new NotImplementedException();
        }

        // Validate the original tx because we're not broadcasting it to check
        // Todo "Is..." Functions return bool. This function expects a string
        // error return type but errors should be thrown as exceptions
        protected override Task<string> IsMempoolEligible(PSBT psbt)
        {
            var validator = psbt.CreateTransactionValidator();
            for (int i = 0; i < psbt.Inputs.Count; i++)
            {
                var res = validator.ValidateInput(i);
                if (res.Error is ScriptError err)
                {
                    return Task.FromResult(err.ToString());
                }
            }
            return Task.FromResult(string.Empty);
        }

        protected override Task<bool> SupportsType(ScriptPubKeyType scriptPubKeyType)
        {
            return Task.FromResult(scriptPubKeyType == ScriptPubKeyType.Segwit);
        }
    }
}
