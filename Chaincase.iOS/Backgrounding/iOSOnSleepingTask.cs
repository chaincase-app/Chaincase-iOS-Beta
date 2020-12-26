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
		Global _global;

		public iOSOnSleepingContext(Global global)
		{
			_global = global;
		}
		public async Task OnSleeping()
		{
			_taskId = UIApplication.SharedApplication.BeginBackgroundTask("OnSleeping", OnExpiration);
			await _global.OnSleeping();
			UIApplication.SharedApplication.EndBackgroundTask(_taskId);
		}

		void OnExpiration()
		{
			UIApplication.SharedApplication.EndBackgroundTask(_taskId);
		}
	}
}
