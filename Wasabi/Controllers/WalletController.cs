using System.IO;
using System.Threading.Tasks;
using NBitcoin;
using WalletWasabi.KeyManagement;

namespace Wasabi.Controllers
{
	public static class GenerateWalletController
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
	}
}
