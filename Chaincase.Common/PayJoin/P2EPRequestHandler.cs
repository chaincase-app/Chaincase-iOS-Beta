using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using NBitcoin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WalletWasabi.Helpers;
using WalletWasabi.JsonConverters;
using WalletWasabi.Wallets;

namespace Chaincase.Common.PayJoin
{
	public class P2EPRequestHandler
	{
		public Network Network { get; }
		public WalletManager WalletManager { get; }
		public INotificationManager NotificationManager { get; private set; }
		public int PrivacyLevelThreshold { get; }

		public P2EPRequestHandler(Network network, WalletManager walletManager, INotificationManager notificationManager, int privacyLevelThreshold = 1)
		{
			Network = network;
			WalletManager = walletManager;
			NotificationManager = notificationManager;
			PrivacyLevelThreshold = privacyLevelThreshold;
		}

		public Task<string> HandleAsync(string body, CancellationToken cancellationToken, string password)
		{
			if (!PSBT.TryParse(body, Network, out var psbt))
			{
				throw new Exception("What the heck are you trying to do?");
			}
			if (!psbt.IsAllFinalized())
			{
				throw new Exception("The PSBT should be finalized");
			}
			// Which coin to use
			var toUse = WalletManager.GetWallets()
				.Where(x => x.State == WalletState.Started && !x.KeyManager.IsWatchOnly && !x.KeyManager.IsHardwareWallet)
				.SelectMany(wallet => wallet.Coins.Select(coin => new { wallet.KeyManager, coin }))
				.Where(x => x.coin.AnonymitySet >= PrivacyLevelThreshold && !x.coin.Unavailable)
				.OrderBy(x => x.coin.IsBanned)
				.ThenBy(x => x.coin.Confirmed)
				.ThenBy(x => x.coin.Height)
				.First();
			// Fees 
			var originalFeeRate = psbt.GetEstimatedFeeRate();
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
			paymentOutput.Value += serverCoin.Amount - originalFeeRate.GetFee(inputSizeInVBytes);

			var newPsbt = PSBT.FromTransaction(paymentTx, Network.Main);
			var serverCoinToSign = newPsbt.Inputs.FindIndexedInput(serverCoin.Outpoint);
			serverCoinToSign.UpdateFromCoin(serverCoin);
			serverCoinToSign.Sign(serverCoinKey.PrivateKey);
			serverCoinToSign.FinalizeInput();

			NotificationManager.ScheduleNotification("PayJoin Received", "The sender has paid you in a CoinJoin style transaction", 1);
			return Task.FromResult(newPsbt.ToHex());
		}
	}
}