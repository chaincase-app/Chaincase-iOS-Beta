using System.IO;
using NBitcoin;
using WalletWasabi.Blockchain.Keys;
using Chaincase.Controllers;
using Xunit;

namespace Chaincase.Tests
{
	public class UnitTests
	{
		[Fact]
		public void AssertGeneratedWalletCredentialsVerifyWithPassphrase()
		{
			string passphrase = "passphrase";
			Mnemonic mnemonic =  WalletController.GenerateMnemonic(passphrase, Network.Main);

			var isVerified =  WalletController.VerifyWalletCredentials(mnemonic.ToString(),  passphrase, Network.Main);
			Assert.True(isVerified);
		}

		[Fact]
		public void AssertGeneratedWalletCredentialsVerifyWithoutPassphrase()
		{
			string passphrase = ""; // cannot be null
			Mnemonic mnemonic =  WalletController.GenerateMnemonic(passphrase, Network.Main);

			var isVerified =  WalletController.VerifyWalletCredentials(mnemonic.ToString(), passphrase, Network.Main);
			Assert.True(isVerified);
		}

		[Fact]
		public void CanLoadKeyManager()
		{
			string walletFilePath = Path.Combine(Global.WalletsDir, $"Main.json");
			KeyManager keyManager = Global.LoadKeyManager(walletFilePath);

			Assert.True(keyManager is KeyManager);
		}		
	}
}
