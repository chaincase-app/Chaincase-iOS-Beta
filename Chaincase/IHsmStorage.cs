using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Chaincase
{
    // Use keys from device Hardware Security Module to encrypt
    public interface IHsmStorage
	{
        // use the most convenient authentication available in implementation
        public Task SetAsync(string key, string value);

        public Task<string> GetAsync(string key);

        public bool Remove(string key);
    }
}
