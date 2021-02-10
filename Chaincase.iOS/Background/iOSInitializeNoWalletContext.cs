using System;
using System.Threading.Tasks;
using Chaincase.Common;
using UIKit;

namespace Chaincase.iOS.Background
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "iOS is iOS")]
    public class iOSInitializeNoWalletContext
    {
        nint _taskId;
        Global _global;

        public iOSInitializeNoWalletContext(Global global)
        {
            _global = global;
        }

        public async Task InitializeNoWallet()
        {
            _taskId = UIApplication.SharedApplication.BeginBackgroundTask("InitializeNoWallet", OnExpiration);

            // cts.Cancel may be required to expire on very old platforms but
            // devices we support on iOS 12+ should be able to handle this long task
            await _global.InitializeNoWalletAsync();

            UIApplication.SharedApplication.EndBackgroundTask(_taskId);
        }

        void OnExpiration()
        {
            UIApplication.SharedApplication.EndBackgroundTask(_taskId);
        }
    }
}
