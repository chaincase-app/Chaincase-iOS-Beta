using System;
using System.Threading;
using System.Threading.Tasks;
using Splat;
using UIKit;
using Xamarin.Forms;

namespace Chaincase.iOS.Backgrounding
{
	public class iOSOnSleepingContext
	{
		nint _taskId;

		public async Task OnSleeping()
		{
			_taskId = UIApplication.SharedApplication.BeginBackgroundTask("OnSleeping", OnExpiration);
			await Locator.Current.GetService<Global>().OnSleeping();
			UIApplication.SharedApplication.EndBackgroundTask(_taskId);
		}

		void OnExpiration()
		{
			UIApplication.SharedApplication.EndBackgroundTask(_taskId);
		}
	}
}
