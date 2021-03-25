using System.Threading.Tasks;
using Microsoft.JSInterop;
using Chaincase.Common.Contracts;

namespace Chaincase.SSB
{
    public sealed class JSClipboard : IClipboard
    {
        private readonly IJSRuntime _jsRuntime;

        public JSClipboard(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string> Paste()
        {
            return await _jsRuntime.InvokeAsync<string>("navigator.clipboard.readText");
        }

        public async Task Copy(string text)
        {
            await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }
}
