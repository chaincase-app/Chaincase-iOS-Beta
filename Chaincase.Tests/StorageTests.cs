using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Chaincase.Common.Services.Mock;
using NBitcoin;
using WalletWasabi.Helpers;
using Xunit;
using Cryptor = Chaincase.Common.Services.AesThenHmac;


namespace Chaincase.Tests
{
    public class StorageTests
    {
        [Fact]
        public async Task CanStoreSeedWords()
        {
            var testDir = EnvironmentHelpers.GetDataDir(Path.Combine("Chaincase", "Tests", "StorageTests"));
            var config = new Config(Path.Combine(testDir, "Config.json"));
            var uiConfig = new UiConfig(Path.Combine(testDir, "UiConfig.json"));

            SensitiveStorage storage = new(new MockHsmStorage(), config, uiConfig);
            string password = "password";
            Mnemonic mnemonic = new(Wordlist.English);
            await storage.SetSeedWords(password, mnemonic.ToString());
            var gotSeedWords = await storage.GetSeedWords(password);
            Assert.True(gotSeedWords == mnemonic.ToString());
        }

		[Fact] 
		 public void CanDecryptCiphertextFromPython()
		{
			// generated with python
			string password = "password";
			string b64CipherText = "5KQS4z/+0xWVGrWXwYtG6BDuQp1k+H+YXxcgIJ2A700uljkJlmmw6sH9//y12f0y2Hxd6AmXKOLPn7Lozfak5RARmv/OmzGt9VvCaHHnFYQ=";
			var plaintext = Cryptor.DecryptWithPassword(b64CipherText, password);
			Assert.True(plaintext == "poops");
		}
	}
}
