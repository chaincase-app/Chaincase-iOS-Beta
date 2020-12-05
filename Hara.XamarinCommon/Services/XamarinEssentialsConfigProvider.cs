using System.Text.Json;
using System.Threading.Tasks;
using Hara.Abstractions.Contracts;
using Xamarin.Essentials;

namespace Hara.XamarinCommon.Services
{
    public class XamarinEssentialsConfigProvider : IConfigProvider
    {
        public Task<T> Get<T>(string key)
        {
            return Task.FromResult(Preferences.ContainsKey(key)
                ? JsonSerializer.Deserialize<T>(Preferences.Get(key, ""))
                : default);
        }

        public Task Set<T>(string key, T value)
        {
            if (value.Equals(default(T)))
            {
                Preferences.Remove(key);
            }
            else
            {
                Preferences.Set(key, JsonSerializer.Serialize(value));
            }

            return Task.CompletedTask;
            ;
        }
    }
}