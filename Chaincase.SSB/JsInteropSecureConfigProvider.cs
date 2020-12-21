using System.Text.Json;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using Chaincase.SSB;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;

public class JsInteropSecureConfigProvider : JsInteropConfigProvider, IHsmStorage
{
	private const string KeyPrefix = "encrypted:";
	private readonly IDataProtector _protector;

	public JsInteropSecureConfigProvider(IDataProtectionProvider dataProtectionProvider, IJSRuntime jsRuntime) :
		base(jsRuntime)
	{
		_protector = dataProtectionProvider.CreateProtector(nameof(JsInteropSecureConfigProvider));
	}

	public override async Task<T> Get<T>(string key)
	{
		var lsRes = await GetRaw($"{KeyPrefix}{key}");

		if (lsRes is null)
		{
			return default(T);
		}

		return JsonSerializer.Deserialize<T>(_protector.Unprotect(lsRes));
	}

	public override async Task Set<T>(string key, T value)
	{
		if (value.Equals(default(T)))
		{
			await base.Set($"{KeyPrefix}{key}", value);
		}
		else
		{
			await SetRaw($"{KeyPrefix}{key}", _protector.Protect(JsonSerializer.Serialize(value)));
		}
	}

	public Task SetAsync(string key, string value)
	{
		return Set(key, value);
	}

	public Task<string> GetAsync(string key)
	{
		return Get<string>(key);
	}

	public bool Remove(string key)
	{
		Set<string>(key, null).GetAwaiter().GetResult();
		return true;
	}
}
