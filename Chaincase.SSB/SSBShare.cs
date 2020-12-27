using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlazorDownloadFile;
using Chaincase.Common.Contracts;
using Microsoft.JSInterop;

namespace Chaincase.SSB
{
	public class SSBShare : IShare
	{
		private readonly IBlazorDownloadFileService _blazorDownloadFileService;
		private readonly IJSRuntime _jsRuntime;

		public SSBShare(IBlazorDownloadFileService blazorDownloadFileService, IJSRuntime jsRuntime)
		{
			_blazorDownloadFileService = blazorDownloadFileService;
			_jsRuntime = jsRuntime;
		}

		public async Task ShareFile(string file, string title)
		{
			var bytes = await File.ReadAllBytesAsync(file);
			await _blazorDownloadFileService.DownloadFile(title, bytes,CancellationToken.None, "application-octet-stream");
		}

		public async Task ShareText(string text, string title = null)
		{
			await _jsRuntime.InvokeAsync<bool>("no share drawer on web", text);
		}
	}
}
