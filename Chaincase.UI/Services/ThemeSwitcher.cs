using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Chaincase.UI.Services
{
	public class ThemeSwitcher
	{
		private readonly IJSRuntime _jsRuntime;

		public ThemeSwitcher(IJSRuntime jsRuntime)
		{
			_jsRuntime = jsRuntime;
		}

		public async Task ToggleDark(bool val)
		{
			await _jsRuntime.InvokeVoidAsync("document.body.classList.toggle", "dark", val);
		}
	}
}
