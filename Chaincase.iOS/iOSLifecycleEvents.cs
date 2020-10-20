using System;
using System.Threading;
using System.Threading.Tasks;
using Splat;
using UIKit;
using Xamarin.Forms;

namespace Chaincase.iOS
{
	public class iOSLifecycleEvents
	{
		nint _taskId;

		public async Task OnSleeping()
		{
			_taskId = UIApplication.SharedApplication.BeginBackgroundTask("OnSleeping", OnExpiration);

			try
			{
				//INVOKE THE SHARED CODE TODO cts.Cancel enable
				await Locator.Current.GetService<Global>().OnSleeping();
			}
			catch (OperationCanceledException)
			{
			}

			UIApplication.SharedApplication.EndBackgroundTask(_taskId);
		}

		void OnExpiration()
		{
			UIApplication.SharedApplication.EndBackgroundTask(_taskId);
		}
	}
}
