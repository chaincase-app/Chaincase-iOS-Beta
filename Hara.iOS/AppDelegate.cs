using Xamarin.Forms;
using Foundation;
using Hara.Abstractions.Contracts;
using Hara.iOS.Services.LocalNotifications.iOS;
using Hara.XamarinCommon;
using Microsoft.Extensions.DependencyInjection;
using UIKit;
using UserNotifications;

namespace Hara.iOS
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

            // For iOS, wrap inside a navigation page, otherwise the header looks wrong
            var formsApp = new App(null,
                collection => { collection.AddSingleton<INotificationManager, iOSNotificationManager>(); });
            UNUserNotificationCenter.Current.Delegate =
                new iOSNotificationReceiver(formsApp.ServiceProvider.GetService<iOSNotificationManager>());
            LoadApplication(formsApp);

            return base.FinishedLaunching(app, options);
        }
    }
}