using System.IO;
using NBitcoin;
using WalletWasabi.KeyManagement;
using Wasabi.Controllers;
using Xunit;

namespace Wasabi.Tests
{
	public class UnitTests
	{
		[Fact]
		public void AssertGeneratedWalletCredentialsVerifyWithPassphrase()
		{
			string passphrase = "passphrase";
			Mnemonic mnemonic =  WalletController.GenerateMnemonicAsync(passphrase).Result;

			var isVerified =  WalletController.VerifyWalletCredentials(mnemonic.ToString(),  passphrase);
			Assert.True(isVerified);
		}

		[Fact]
		public void AssertGeneratedWalletCredentialsVerifyWithoutPassphrase()
		{
			string passphrase = ""; // cannot be null
			Mnemonic mnemonic =  WalletController.GenerateMnemonicAsync(passphrase).Result;

			var isVerified =  WalletController.VerifyWalletCredentials(mnemonic.ToString(), passphrase);
			Assert.True(isVerified);
		}

		[Fact]
		public void CanLoadKeyManager()
		{
			string walletFilePath = Path.Combine(Global.WalletsDir, $"Main.json");
			KeyManager keyManager = Global.LoadKeyManager(walletFilePath, walletFilePath);

			Assert.True(keyManager is KeyManager);
		}
	}
}
