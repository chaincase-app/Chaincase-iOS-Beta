using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using Xamarin.Essentials;

namespace Chaincase.Services
{
	public class XamarinClipboard : IClipboard
	{
		public async Task Copy(string text)
		{
			await Clipboard.SetTextAsync(text);
		}

		public async Task<string> Paste()
		{
			return await Clipboard.GetTextAsync();
		}
	}
}
