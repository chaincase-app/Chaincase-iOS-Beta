using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using WalletWasabi.KeyManagement;
using WalletWasabi.Logging;
using Wasabi.ViewModels;

namespace Wasabi.Controllers
{
	public static class WalletController
	{
		public static Mnemonic GenerateMnemonic(string passphrase)
		{
			string walletFilePath = Path.Combine(Global.WalletsDir, $"{Global.Network.ToString()}.json");
			KeyManager.CreateNew(out Mnemonic mnemonic, passphrase, walletFilePath);
			return mnemonic;
		}

		public static bool VerifyWalletCredentials(string mnemonicString, string passphrase)
		{
			Mnemonic mnemonic = new Mnemonic(mnemonicString);
			ExtKey derivedExtKey = mnemonic.DeriveExtKey(passphrase);

			string walletFilePath = Path.Combine(Global.WalletsDir, $"{Global.Network.ToString()}.json");
			var keyOnDisk = KeyManager.FromFile(walletFilePath).GetMasterExtKey(passphrase);

			return keyOnDisk.Equals(derivedExtKey);
		}

		public static async Task LoadWalletAsync()
		{
			// TODO Nono backup wallet folder!!
			string walletFilePath = Global.GetWalletFullPath(Global.Network.ToString());
			KeyManager keyManager = Global.LoadKeyManager(walletFilePath);
			try
			{
				await Global.InitializeWalletServiceAsync(keyManager);
			}
			catch (Exception ex)
			{
				// Initialization failed.
				Logger.LogError<ReceiveViewModel>(ex);
				await Global.DisposeInWalletDependentServicesAsync();
			}
		}

		public static bool WalletExists()
		{
			string walletFilePath = Global.GetWalletFullPath(Global.Network.ToString());
			return File.Exists(walletFilePath);
		}

		public static async Task<Money> GetBalanceAsync()
		{
			return await Task.Run(() =>
			{
				return Enumerable.Where
				(
					Global.WalletService.Coins,
					c => c.Unspent && !c.SpentAccordingToBackend
				).Sum(c => (long?)c.Amount) ?? 0;
			});
		}
	}
}
