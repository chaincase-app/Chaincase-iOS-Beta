using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using Microsoft.JSInterop;

namespace Chaincase.SSB
{
	public class WebCameraScanner : ICameraScanner
	{
		private readonly IJSRuntime _jsRuntime;
		private TaskCompletionSource<string> tcs;

		public WebCameraScanner(IJSRuntime jsRuntime)
		{
			_jsRuntime = jsRuntime;
		}

		public async Task<string> Scan()
		{
			return (await _jsRuntime.InvokeAsync<string>("IonicBridge.executeFunctionByName", "window", "prompt", "paste your scan"));
		}
	}
}
