using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using NBitcoin;
using Xunit;

namespace Chaincase.Tests
{
	public class StorageTests
    {
        [Fact]
        public void CanStoreSeedWords()
        {
            var mockConfig = new Config("");
            SensitiveStorage storage = new(new MockHsm(), mockConfig);
            string password = "password";
            Mnemonic mnemonic = new(Wordlist.English);
            storage.SetSeedWords(password, mnemonic.ToString());
            var gotSeedWords = storage.GetSeedWords(password).Result;
            Assert.True(gotSeedWords == mnemonic.ToString());
        }
    }

    class MockHsm : IHsmStorage
    {
        private Dictionary<string, string> keyStore = new();

        public Task<string> GetAsync(string key)
        {
            var tcs = new TaskCompletionSource<string>();
            if (!keyStore.TryGetValue(key, out var value))
                throw new Exception();
            tcs.SetResult(value);
            return tcs.Task;
        }

        public bool Remove(string key)
        {
            return true;
        }

        public Task SetAsync(string key, string value)
        {
            keyStore.AddOrReplace(key, value);
            return Task.CompletedTask;
        }
    }
}
