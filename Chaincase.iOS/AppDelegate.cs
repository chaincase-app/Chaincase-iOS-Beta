using System;
using System.Collections.Generic;
using System.Linq;
using Chaincase.Common.Xamarin;
using Xamarin.Forms;

using Foundation;
using Microsoft.Extensions.DependencyInjection;
using UIKit;
using UserNotifications;
using Splat;
using System.Threading.Tasks;
using Chaincase.Background;

namespace Chaincase.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            UNUserNotificationCenter.Current.Delegate = new iOSNotificationReceiver();
            ZXing.Net.Mobile.Forms.iOS.Platform.Init();

            var formsApp = new App(ConfigureDi);

            MessagingCenter.Subscribe<StartOnSleepingTaskMessage>(this, "StartOnSleepingTaskMessage", async message => {
                var lifecycle = new iOSLifecycleEvents();
                await lifecycle.OnSleeping();
            });

            nint taskID = 0;

            // register a long running task, and then start it on a new thread so that this method can return
            taskID = UIApplication.SharedApplication.BeginBackgroundTask(() =>
            {
                Console.WriteLine("Running out of time to complete you background task!");
                UIApplication.SharedApplication.EndBackgroundTask(taskID);
            });

            Task.Factory.StartNew(() => FinishLongRunningTask(taskID));

            LoadApplication(formsApp);

            return base.FinishedLaunching(app, options);
        }

        private void ConfigureDi(IServiceCollection obj)
        {
            obj.ConfigureCommonXamarinServices();
        }

        private void FinishLongRunningTask(nint taskID)
        {
            Console.WriteLine($"Starting task {taskID}");
            Console.WriteLine($"Background time remaining: {UIApplication.SharedApplication.BackgroundTimeRemaining}");

            Locator.Current.GetService<Global>().InitializeNoWalletAsync();

            Console.WriteLine($"Task {taskID} finished");
            Console.WriteLine($"Background time remaining: {UIApplication.SharedApplication.BackgroundTimeRemaining}");

            // call our end task
            UIApplication.SharedApplication.EndBackgroundTask(taskID);
        }
    }
}
