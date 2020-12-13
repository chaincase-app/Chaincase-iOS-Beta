using System;
using System.Threading.Tasks;
using Chaincase.Common;
using Splat;
using UIKit;

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
