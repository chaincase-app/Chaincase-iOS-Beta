using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlazorDownloadFile;
using Chaincase.Common.Contracts;

namespace Chaincase.SSB
{
	public class SSBFileShare : IShare
	{
		private readonly IBlazorDownloadFileService _blazorDownloadFileService;

		public SSBFileShare(IBlazorDownloadFileService blazorDownloadFileService)
		{
			_blazorDownloadFileService = blazorDownloadFileService;
		}
		public async Task ShareFile(string file, string title)
		{
			var bytes = await File.ReadAllBytesAsync(file);
			await _blazorDownloadFileService.DownloadFile(title, bytes,CancellationToken.None, "application-octet-stream");
		}
	}
}
