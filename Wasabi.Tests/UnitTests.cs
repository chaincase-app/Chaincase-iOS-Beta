using NBitcoin;
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
			Mnemonic mnemonic = GenerateWalletController.GenerateMnemonicAsync(passphrase).Result;

			var isVerified = GenerateWalletController.VerifyWalletCredentials(mnemonic.ToString(),  passphrase);
			Assert.True(isVerified);
		}

		[Fact]
		public void AssertGeneratedWalletCredentialsVerifyWithoutPassphrase()
		{
			string passphrase = ""; // cannot be null
			Mnemonic mnemonic = GenerateWalletController.GenerateMnemonicAsync(passphrase).Result;

			var isVerified = GenerateWalletController.VerifyWalletCredentials(mnemonic.ToString(), passphrase);
			Assert.True(isVerified);
		}
	}
}
