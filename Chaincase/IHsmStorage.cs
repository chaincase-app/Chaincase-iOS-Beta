using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Chaincase
{
    // Use keys from device Hardware Security Module to encrypt
    public interface IHsmStorage
	{
        public Task SetWithPasswordAsync(string key, string value);

        public bool Remove(string key);
    }
}
