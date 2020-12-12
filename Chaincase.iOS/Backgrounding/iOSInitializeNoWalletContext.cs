using System;
using System.Threading.Tasks;
using Chaincase.Common;
using Splat;
using UIKit;

namespace Chaincase.iOS.Backgrounding
{
	public class iOSInitializeNoWalletContext
	{
		nint _taskId;

		public async Task InitializeNoWallet()
		{
			_taskId = UIApplication.SharedApplication.BeginBackgroundTask("InitializeNoWallet", OnExpiration);


			// cts.Cancel may be required to expire on very old platforms but
			// devices we support on iOS 12+ should be able to handle this long task
			await Locator.Current.GetService<Global>().InitializeNoWalletAsync();

			UIApplication.SharedApplication.EndBackgroundTask(_taskId);
		}

		void OnExpiration()
		{
			UIApplication.SharedApplication.EndBackgroundTask(_taskId);
		}
	}
}
