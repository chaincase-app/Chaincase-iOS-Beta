using System;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Chaincase.Services
{
	public class XamarinMainThreadInvoker : IMainThreadInvoker
	{
		public void Invoke(Action action)
		{
			Device.BeginInvokeOnMainThread(action);
		}
	}

	public class XamarinFileShare : IFileShare
	{
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
