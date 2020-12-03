using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Chaincase.Common.Xamarin
{
    public class XamarinHsmStorage : IHsmStorage
    {
        public Task SetAsync(string key, string value)
        {
            return SecureStorage.SetAsync(key, value);
        }

        public Task<string> GetAsync(string key)
        {
            return SecureStorage.GetAsync(key);
        }

        public bool Remove(string key)
        {
            return SecureStorage.Remove(key);
        }
    }
}