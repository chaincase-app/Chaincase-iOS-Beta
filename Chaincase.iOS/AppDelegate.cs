using System;
using System.Collections.Generic;
using System.Linq;
using Chaincase.Common.Xamarin;
using Xamarin.Forms;

using Foundation;
using Microsoft.Extensions.DependencyInjection;
using UIKit;
using UserNotifications;

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

            LoadApplication(formsApp);

            return base.FinishedLaunching(app, options);
        }

        private void ConfigureDi(IServiceCollection obj)
        {
            obj.ConfigureCommonXamarinServices();
        }
    }
}
