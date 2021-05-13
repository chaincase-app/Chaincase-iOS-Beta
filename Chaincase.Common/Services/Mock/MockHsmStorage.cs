using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using NBitcoin;

namespace Chaincase.Common.Services.Mock
{
    public class MockHsmStorage : IHsmStorage
    {
        private readonly Dictionary<string, string> keyStore = new();

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
