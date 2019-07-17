using System;
using System.IO;
using System.Threading.Tasks;
using NBitcoin;
using WalletWasabi.KeyManagement;
using WalletWasabi.Logging;
using Wasabi.ViewModels;

namespace Wasabi.Controllers
{
	public static class WalletController
	{
		public static Task<Mnemonic> GenerateMnemonicAsync(string passphrase)
		{
			return Task.Run(() =>
			{
				string walletFilePath = Path.Combine(Global.WalletsDir, $"Main.json");
				KeyManager.CreateNew(out Mnemonic mnemonic, passphrase, walletFilePath);
				return mnemonic;
			});
		}

		public static bool VerifyWalletCredentials(string mnemonicString, string passphrase)
		{
			Mnemonic mnemonic = new Mnemonic(mnemonicString);
			ExtKey derivedExtKey = mnemonic.DeriveExtKey(passphrase);

			string walletFilePath = Path.Combine(Global.WalletsDir, $"Main.json");
			var keyOnDisk = KeyManager.FromFile(walletFilePath).GetMasterExtKey(passphrase);

			return keyOnDisk.Equals(derivedExtKey);
		}

		public static async Task LoadWalletAsync()
		{
			// TODO Nono backup wallet folder!!
			string walletFilePath = Global.GetWalletFullPath("Main");
			KeyManager keyManager = Global.LoadKeyManager(walletFilePath, walletFilePath);
			try
			{
				Global.InitializeWalletServiceAsync(keyManager);
			}
			catch (Exception ex)
			{
				// Initialization failed.
				Logger.LogError<ReceiveViewModel>(ex);
				await Global.DisposeInWalletDependentServicesAsync();
			}
		}
	}
}
