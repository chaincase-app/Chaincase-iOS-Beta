using System;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Chaincase.Services
{
	public class XamarinShare : IShare
	{
		public async Task ShareText(string text, string title = "Share")
		{
			await Share.RequestAsync(new ShareTextRequest
			{
				Title = title,
				Text = text
			});
		}
		public async Task ShareFile(string file, string title)
		{
			await Share.RequestAsync(new ShareFileRequest
			{
				Title = title,
				File = new ShareFile(file)
			});
		}
	}
}
