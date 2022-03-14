using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BTCPayServer.BIP78.Sender;
using Chaincase.Common.Contracts;
using NBitcoin;
using NBitcoin.Payment;
using NBitcoin.Policy;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.TransactionProcessing;
using WalletWasabi.CoinJoin.Client.Clients.Queuing;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.Wallets;

namespace Chaincase.Common.Services
{
    public class ChaincaseWalletManager : WalletManager
    {
        private readonly INotificationManager _notificationManager;
        private readonly PayjoinClient _payjoinClient;

        public Wallet CurrentWallet { get; set; }
        public IEnumerable<SmartCoin> SleepingCoins;

        public ChaincaseWalletManager(Network network, WalletDirectories walletDirectories, INotificationManager notificationManager, PayjoinClient payjoinClient)
            : base(network, walletDirectories)
        {
            _notificationManager = notificationManager;
            _payjoinClient = payjoinClient;

            OnDequeue += WalletManager_OnDequeue;
            WalletRelevantTransactionProcessed += WalletManager_WalletRelevantTransactionProcessed;
        }

        private void WalletManager_OnDequeue(object? sender, DequeueResult e)
        {
            try
            {
                foreach (var success in e.Successful.Where(x => x.Value.Any()))
                {
                    DequeueReason reason = success.Key;
                    if (reason == DequeueReason.ApplicationExit)
                    {
                        SleepingCoins = success.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex);
            }
        }

        private void WalletManager_WalletRelevantTransactionProcessed(object sender, ProcessedResult e)
        {
            try
            {
                // If there are no news, then don't bother.
                if (!e.IsNews || (sender as Wallet).State != WalletState.Started)
                {
                    return;
                }

                // ToDo
                // Double spent.
                // Anonymity set gained?
                // Received dust

                bool isSpent = e.NewlySpentCoins.Any();
                bool isReceived = e.NewlyReceivedCoins.Any();
                bool isConfirmedReceive = e.NewlyConfirmedReceivedCoins.Any();
                bool isConfirmedSpent = e.NewlyConfirmedReceivedCoins.Any();
                Money miningFee = e.Transaction.Transaction.GetFee(e.SpentCoins.Select(x => x.GetCoin()).ToArray());
                if (isReceived || isSpent)
                {
                    Money receivedSum = e.NewlyReceivedCoins.Sum(x => x.Amount);
                    Money spentSum = e.NewlySpentCoins.Sum(x => x.Amount);
                    Money incoming = receivedSum - spentSum;
                    Money receiveSpentDiff = incoming.Abs();
                    string amountString = receiveSpentDiff.ToString(false, true);

                    if (e.Transaction.Transaction.IsCoinBase)
                    {
                        _notificationManager.NotifyAndLog($"{amountString} BTC", "Mined", NotificationType.Success, e);
                    }
                    else if (isSpent && receiveSpentDiff == miningFee)
                    {
                        _notificationManager.NotifyAndLog($"Mining Fee: {amountString} BTC", "Self Spend", NotificationType.Information, e);
                    }
                    else if (isSpent && receiveSpentDiff.Almost(Money.Zero, Money.Coins(0.01m)) && e.IsLikelyOwnCoinJoin)
                    {
                        _notificationManager.NotifyAndLog($"CoinJoin Completed!", "", NotificationType.Success, e);
                    }
                    else if (incoming > Money.Zero)
                    {
                        if (e.Transaction.IsRBF && e.Transaction.IsReplacement)
                        {
                            _notificationManager.NotifyAndLog($"{amountString} BTC", "Received Replaceable Replacement Transaction", NotificationType.Information, e);
                        }
                        else if (e.Transaction.IsRBF)
                        {
                            _notificationManager.NotifyAndLog($"{amountString} BTC", "Received Replaceable Transaction", NotificationType.Success, e);
                        }
                        else if (e.Transaction.IsReplacement)
                        {
                            _notificationManager.NotifyAndLog($"{amountString} BTC", "Received Replacement Transaction", NotificationType.Information, e);
                        }
                        else
                        {
                            _notificationManager.NotifyAndLog($"{amountString} BTC", "Received", NotificationType.Success, e);
                        }
                    }
                    else if (incoming < Money.Zero)
                    {
                        _notificationManager.NotifyAndLog($"{amountString} BTC", "Sent", NotificationType.Information, e);
                    }
                }
                else if (isConfirmedReceive || isConfirmedSpent)
                {
                    Money receivedSum = e.ReceivedCoins.Sum(x => x.Amount);
                    Money spentSum = e.SpentCoins.Sum(x => x.Amount);
                    Money incoming = receivedSum - spentSum;
                    Money receiveSpentDiff = incoming.Abs();
                    string amountString = receiveSpentDiff.ToString(false, true);

                    if (isConfirmedSpent && receiveSpentDiff == miningFee)
                    {
                        _notificationManager.NotifyAndLog($"Mining Fee: {amountString} BTC", "Self Spend Confirmed", NotificationType.Information, e);
                    }
                    else if (isConfirmedSpent && e.IsLikelyOwnCoinJoin)
                    {
                        _notificationManager.NotifyAndLog($"CoinJoin Confirmed!", "", NotificationType.Information, e);
                    }
                    else if (incoming > Money.Zero)
                    {
                        _notificationManager.NotifyAndLog($"{amountString} BTC", "Receive Confirmed", NotificationType.Information, e);
                    }
                    else if (incoming < Money.Zero)
                    {
                        _notificationManager.NotifyAndLog($"{amountString} BTC", "Send Confirmed", NotificationType.Information, e);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex);
            }
        }

        public void SetDefaultWallet()
        {
            CurrentWallet = GetWalletByName(Network.ToString());
        }

        public bool HasDefaultWalletFile()
        {
            // this is kinda codesmell biz logic but it doesn't make sense for a full VM here
            var walletName = Network.ToString();
            (string walletFullPath, _) = WalletDirectories.GetWalletFilePaths(walletName);
            return File.Exists(walletFullPath);
        }

        public Task<PSBT> BuildPayjoin(
            PaymentIntent payments,
            IEnumerable<OutPoint> coinsToSpend,
            string password = "",
            bool allowUnconfirmed = false)
        {
            payments = Guard.NotNull(nameof(payments), payments);

            // was a <param>
            Func<LockTime> lockTimeSelector = null;
            lockTimeSelector ??= () => LockTime.Zero;

            long totalAmount = payments.TotalAmount.Satoshi;
            if (totalAmount < 0 || totalAmount > Constants.MaximumNumberOfSatoshis)
            {
                throw new ArgumentOutOfRangeException($"{nameof(payments)}.{nameof(payments.TotalAmount)} sum cannot be smaller than 0 or greater than {Constants.MaximumNumberOfSatoshis}.");
            }

            // 1. Get allowed coins to spend.
            var availableCoinsView = CurrentWallet.Coins.Available();
            // TODO Kill SmartCoin
            // TODO This whole logic is a dirty abstraction. Just pass enough information to spend
            // TODO It doesn't make sense for this function to have access to that state, it should be passed in
            List<SmartCoin> allowedSmartCoinInputs = allowUnconfirmed // Inputs that can be used to build the transaction.
                    ? availableCoinsView.ToList()
                    : availableCoinsView.Confirmed().ToList();
            if (coinsToSpend != null) // If allowedInputs are specified then select the coins from them.
            {
                if (!coinsToSpend.Any())
                {
                    throw new ArgumentException($"{nameof(coinsToSpend)} is not null, but empty.");
                }

                allowedSmartCoinInputs = allowedSmartCoinInputs
                    .Where(x => coinsToSpend.Any(y => y.Hash == x.TransactionId && y.N == x.Index))
                    .ToList();

                // Add those that have the same script, because common ownership is already exposed.
                // But only if the user didn't click the "max" button. In this case he'd send more money than what he'd think.
                if (payments.ChangeStrategy != ChangeStrategy.AllRemainingCustom)
                {
                    var allScripts = allowedSmartCoinInputs.Select(x => x.ScriptPubKey).ToHashSet();
                    foreach (var coin in availableCoinsView.Where(x => !allowedSmartCoinInputs.Any(y => x.TransactionId == y.TransactionId && x.Index == y.Index)))
                    {
                        if (!(allowUnconfirmed || coin.Confirmed))
                        {
                            continue;
                        }

                        if (allScripts.Contains(coin.ScriptPubKey))
                        {
                            allowedSmartCoinInputs.Add(coin);
                        }
                    }
                }
            }

            // Get and calculate fee
            Logger.LogInfo("Calculating dynamic transaction fee...");

            TransactionBuilder builder = Network.CreateTransactionBuilder();
            builder.SetCoinSelector(new SmartCoinSelector(allowedSmartCoinInputs));
            builder.AddCoins(allowedSmartCoinInputs.Select(c => c.GetCoin()));
            builder.SetLockTime(lockTimeSelector());

            foreach (var request in payments.Requests.Where(x => x.Amount.Type == MoneyRequestType.Value))
            {
                var amountRequest = request.Amount;

                builder.Send(request.Destination, amountRequest.Amount);
                if (amountRequest.SubtractFee)
                {
                    builder.SubtractFees();
                }
            }

            // TODO could this be passed into the function instead of knoing about (TODO rm) KeyManager
            HdPubKey changeHdPubKey = null;

            if (payments.TryGetCustomRequest(out DestinationRequest custChange))
            {
                var changeScript = custChange.Destination.ScriptPubKey;
                // TODO rm KeyManager
                changeHdPubKey = CurrentWallet.KeyManager.GetKeyForScriptPubKey(changeScript);

                var changeStrategy = payments.ChangeStrategy;
                if (changeStrategy == ChangeStrategy.Custom)
                {
                    builder.SetChange(changeScript);
                }
                else if (changeStrategy == ChangeStrategy.AllRemainingCustom)
                {
                    builder.SendAllRemaining(changeScript);
                }
                else
                {
                    throw new NotSupportedException(payments.ChangeStrategy.ToString());
                }
            }
            else
            {
                CurrentWallet.KeyManager.AssertCleanKeysIndexed(isInternal: true);
                CurrentWallet.KeyManager.AssertLockedInternalKeysIndexed(14);
                changeHdPubKey = CurrentWallet.KeyManager.GetKeys(KeyState.Clean, true).RandomElement();

                builder.SetChange(changeHdPubKey.P2wpkhScript);
            }

            builder.OptInRBF = new Random().NextDouble() < Constants.TransactionRBFSignalRate;

            FeeRate feeRate = feeRateFetcher();
            builder.SendEstimatedFees(feeRate);

            var psbt = builder.BuildPSBT(false);

            var spentCoins = psbt.Inputs.Select(txin => allowedSmartCoinInputs.First(y => y.OutPoint == txin.PrevOut)).ToArray();

            var realToSend = payments.Requests
                .Select(t =>
                    (label: t.Label,
                    destination: t.Destination,
                    amount: psbt.Outputs.FirstOrDefault(o => o.ScriptPubKey == t.Destination.ScriptPubKey)?.Value))
                .Where(i => i.amount != null);

            if (!psbt.TryGetFee(out var fee))
            {
                throw new InvalidOperationException("Impossible to get the fees of the PSBT, this should never happen.");
            }
            Logger.LogInfo($"Fee: {fee.Satoshi} Satoshi.");

            var vSize = builder.EstimateSize(psbt.GetOriginalTransaction(), true);
            Logger.LogInfo($"Estimated tx size: {vSize} vBytes.");

            // Do some checks
            Money totalSendAmountNoFee = realToSend.Sum(x => x.amount);
            if (totalSendAmountNoFee == Money.Zero)
            {
                throw new InvalidOperationException("The amount after subtracting the fee is too small to be sent.");
            }

            Money totalOutgoingAmountNoFee;
            if (changeHdPubKey is null)
            {
                totalOutgoingAmountNoFee = totalSendAmountNoFee;
            }
            else
            {
                totalOutgoingAmountNoFee = realToSend.Where(x => !changeHdPubKey.ContainsScript(x.destination.ScriptPubKey)).Sum(x => x.amount);
            }
            decimal totalOutgoingAmountNoFeeDecimal = totalOutgoingAmountNoFee.ToDecimal(MoneyUnit.BTC);
            // Cannot divide by zero, so use the closest number we have to zero.
            decimal totalOutgoingAmountNoFeeDecimalDivisor = totalOutgoingAmountNoFeeDecimal == 0 ? decimal.MinValue : totalOutgoingAmountNoFeeDecimal;
            decimal feePc = 100 * fee.ToDecimal(MoneyUnit.BTC) / totalOutgoingAmountNoFeeDecimalDivisor;

            if (feePc > 1)
            {
                Logger.LogInfo($"The transaction fee is {feePc:0.#}% of the sent amount.{Environment.NewLine}"
                    + $"Sending:\t {totalOutgoingAmountNoFee.ToString(fplus: false, trimExcessZero: true)} BTC.{Environment.NewLine}"
                    + $"Fee:\t\t {fee.Satoshi} Satoshi.");
            }
            if (feePc > 100)
            {
                throw new InvalidOperationException($"The transaction fee is more than twice the sent amount: {feePc:0.#}%.");
            }

            if (spentCoins.Any(u => !u.Confirmed))
            {
                Logger.LogInfo("Unconfirmed transaction is spent.");
            }

            // Build the transaction
            Logger.LogInfo("Signing transaction...");
            // It must be watch only, too, because if we have the key and also hardware wallet, we do not care we can sign.

            Transaction tx = null;
            if (CurrentWallet.KeyManager.IsWatchOnly)
            {
                tx = psbt.GetGlobalTransaction();
            }
            else
            {
                IEnumerable<ExtKey> signingKeys = CurrentWallet.KeyManager.GetSecrets(password, spentCoins.Select(x => x.ScriptPubKey).ToArray());
                builder = builder.AddKeys(signingKeys.ToArray());
                builder.SignPSBT(psbt);

                UpdatePSBTInfo(psbt, spentCoins, changeHdPubKey);

                if (!CurrentWallet.KeyManager.IsWatchOnly)
                {
                    // Try to pay using payjoin
                    if (_payjoinClient is { })
                    {
                        psbt = TryNegotiatePayjoin(payjoinClient, builder, psbt, changeHdPubKey);
                    }
                }
                psbt.Finalize();
                tx = psbt.ExtractTransaction();

                var checkResults = builder.Check(tx).ToList();
                if (!psbt.TryGetEstimatedFeeRate(out FeeRate actualFeeRate))
                {
                    throw new InvalidOperationException("Impossible to get the fee rate of the PSBT, this should never happen.");
                }

                // Manually check the feerate, because some inaccuracy is possible.
                var sb1 = feeRate.SatoshiPerByte;
                var sb2 = actualFeeRate.SatoshiPerByte;
                if (Math.Abs(sb1 - sb2) > 2) // 2s/b inaccuracy ok.
                {
                    // So it'll generate a transactionpolicy error thrown below.
                    checkResults.Add(new NotEnoughFundsPolicyError("Fees different than expected"));
                }
                if (checkResults.Count > 0)
                {
                    throw new InvalidTxException(tx, checkResults);
                }
                //if (feeStrategy.Type == FeeStrategyType.Target)
                //{
                //    return FeeProvider.AllFeeEstimate?.GetFeeRate(feeStrategy.Target) ?? throw new InvalidOperationException("Cannot get fee estimations.");
                //}
                //else if (feeStrategy.Type == FeeStrategyType.Rate)
                //{
                //    return feeStrategy.Rate;
                //}
                //else
                //{
                //    throw new NotSupportedException(feeStrategy.Type.ToString());
                //}
                return Task.CompletedTask;
            }
        }

        // <summary> Just a helper method </summary>
        private PSBT TryNegotiatePayjoin(BitcoinUrlBuilder bip21, TransactionBuilder builder, PSBT psbt, HdPubKey changeHdPubKey)
        {
            try
            {
                bip21.TryGetPayjoinEndpoint(out var endpoint);
                Logger.LogInfo($"Negotiating payjoin payment with `{endpoint}`.");

                psbt = _payjoinClient.RequestPayjoin(bip21, new PayjoinWallet());
                //psbt = payjoinClient.RequestPayjoin(psbt,
                //	KeyManager.ExtPubKey,
                //	new RootedKeyPath(KeyManager.MasterFingerprint.Value, KeyManager.DefaultAccountKeyPath),
                //	changeHdPubKey,
                //	CancellationToken.None).GetAwaiter().GetResult();
                builder.SignPSBT(psbt);

                Logger.LogInfo($"Payjoin payment was negotiated successfully.");
            }
            catch (TorSocks5FailureResponseException e)
            {
                if (e.Message.Contains("HostUnreachable"))
                {
                    Logger.LogWarning($"Payjoin server is not reachable. Ignoring...");
                }
                // ignore
            }
            catch (HttpRequestException e)
            {
                Logger.LogWarning($"Payjoin server responded with {e.ToTypeMessageString()}. Ignoring...");
            }
            catch (PayjoinException e)
            {
                Logger.LogWarning($"Payjoin server responded with {e.Message}. Ignoring...");
            }

            return psbt;
        }

        private void UpdatePSBTInfo(PSBT psbt, SmartCoin[] spentCoins, HdPubKey changeHdPubKey)
        {
            if (CurrentWallet.KeyManager.MasterFingerprint is HDFingerprint fp)
            {
                foreach (var coin in spentCoins)
                {
                    var rootKeyPath = new RootedKeyPath(fp, coin.HdPubKey.FullKeyPath);
                    psbt.AddKeyPath(coin.HdPubKey.PubKey, rootKeyPath, coin.ScriptPubKey);
                }
                if (changeHdPubKey is { })
                {
                    var rootKeyPath = new RootedKeyPath(fp, changeHdPubKey.FullKeyPath);
                    psbt.AddKeyPath(changeHdPubKey.PubKey, rootKeyPath, changeHdPubKey.P2wpkhScript);
                }
            }

            foreach (var input in spentCoins)
            {
                var coinInputTxID = input.TransactionId;
                if (CurrentWallet.BitcoinStore.TransactionStore.TryGetTransaction(coinInputTxID, out var txn))
                {
                    var psbtInputs = psbt.Inputs.Where(x => x.PrevOut.Hash == coinInputTxID);
                    foreach (var psbtInput in psbtInputs)
                    {
                        psbtInput.NonWitnessUtxo = txn.Transaction;
                    }
                }
                else
                {
                    Logger.LogWarning($"Transaction id:{coinInputTxID} is missing from the TransactionStore. Ignoring...");
                }
            }
        }
    }
