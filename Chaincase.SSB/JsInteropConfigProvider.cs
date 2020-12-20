using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Chaincase.SSB
{
	public class JsInteropConfigProvider
	{
		private readonly IJSRuntime _jsRuntime;

		public JsInteropConfigProvider(IJSRuntime jsRuntime)
		{
			_jsRuntime = jsRuntime;
		}

		public virtual async Task<T> Get<T>(string key)
		{
			var lsRes = await GetRaw(key);

			if (lsRes is null)
			{
				return default(T);
			}

			return JsonSerializer.Deserialize<T>(lsRes);
		}

		protected async Task<string> GetRaw(string key)
		{
			return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
		}

		public virtual async Task Set<T>(string key, T value)
		{
			if (value.Equals(default(T)))
			{
				await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
			}
			else
			{
				await SetRaw(key, JsonSerializer.Serialize(value));
			}
		}

		protected async Task SetRaw(string key, string value)
		{
			await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
		}
	}
}
