using NBitcoin;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Logging;
using WalletWasabi.Wallets;

namespace Chaincase.Controllers
{
	public static class WalletController
	{
		public static Mnemonic GenerateMnemonic(string passphrase, NBitcoin.Network network)
		{
			string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{network}.json");
			KeyManager.CreateNew(out Mnemonic mnemonic, passphrase, walletFilePath);
			return mnemonic;
		}

		public static bool VerifyWalletCredentials(string mnemonicString, string passphrase, NBitcoin.Network network)
		{
			Mnemonic mnemonic = new Mnemonic(mnemonicString);
			ExtKey derivedExtKey = mnemonic.DeriveExtKey(passphrase);

			string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{network}.json");
			ExtKey keyOnDisk;
			try
			{
				keyOnDisk = KeyManager.FromFile(walletFilePath).GetMasterExtKey(passphrase);
			}
			catch
			{
				// bad password
				return false;
			}
			return keyOnDisk.Equals(derivedExtKey);
		}

		public static string IsValidPassword(string pass, Network network)
		{
			string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{network}.json");
			ExtKey keyOnDisk;
			try
			{
				keyOnDisk = KeyManager.FromFile(walletFilePath).GetMasterExtKey(pass);
			}
			catch
			{
				// bad password
				return null;
			}
			return pass;
		}

		public static async Task LoadWalletAsync(Network network)
		{
			string walletName = network.ToString();
			KeyManager keyManager = Global.WalletManager.GetWalletByName(walletName).KeyManager;
			if (keyManager is null)
			{
				return;
			}

			try
			{
				var wallet = await Global.WalletManager.StartWalletAsync(keyManager);
				// Successfully initialized.
			}
			catch (OperationCanceledException ex)
			{
				Logger.LogTrace(ex);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
			}
		}

		public static bool WalletExists(Network network)
		{
			var walletName = network.ToString();
			(string walletFullPath, _) = Global.WalletManager.WalletDirectories.GetWalletFilePaths(walletName);
			return File.Exists(walletFullPath);
		}

		public static Money GetBalance(Network network)
		{
			return Enumerable.Where
				(
					Global.WalletManager.GetWalletByName(network.ToString()).Coins,
					c => c.Unspent && !c.SpentAccordingToBackend
				).Sum(c => (long?)c.Amount) ?? 0;
		}

        public static Money GetPrivateBalance(Network network)
        {
			return Enumerable.Where
				(
					Global.WalletManager.GetWalletByName(network.ToString()).Coins,
					c => c.Unspent && !c.SpentAccordingToBackend && c.AnonymitySet > 1
				).Sum(c => (long?)c.Amount) ?? 0;
		}

		public static bool SendTransaction(string addressString, FeeRate rate)
		{
			BitcoinAddress address;
			try
			{
				address = BitcoinAddress.Create(addressString.Trim(), Global.Network);
			}
			catch (FormatException e)
			{
				return false;
			}

			return true;
		}
	}
}
