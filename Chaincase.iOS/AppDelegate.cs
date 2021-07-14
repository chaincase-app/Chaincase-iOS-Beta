using Chaincase.Background;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.iOS.Services;
using Chaincase.iOS.Background;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using UIKit;
using UserNotifications;
using Xamarin.Forms;
using Splat;
using WalletWasabi.Logging;
using CoreFoundation;
using ObjCRuntime;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Chaincase.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {

        private IServiceProvider ServiceProvider;
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

            ZXing.Net.Mobile.Forms.iOS.Platform.Init();
            var formsApp = new BlazorApp(fileProvider: null, ConfigureDi);
            var serviceProvider = formsApp.ServiceProvider;
            ServiceProvider = serviceProvider;
            MessagingCenter.Subscribe<InitializeNoWalletTaskMessage>(this, "InitializeNoWalletTaskMessage", async message =>
            {
                var context = new iOSInitializeNoWalletContext(serviceProvider.GetService<Global>());
                await context.InitializeNoWallet();
            });

            MessagingCenter.Subscribe<OnSleepingTaskMessage>(this, "OnSleepingTaskMessage", async message =>
            {
                var context = new iOSOnSleepingContext(serviceProvider.GetService<Global>());
                await context.OnSleeping();
            });

            formsApp.InitializeNoWallet();

            UNUserNotificationCenter.Current.Delegate =
                formsApp.ServiceProvider.GetService<iOSNotificationReceiver>();
            LoadApplication(formsApp);
            UIApplication.SharedApplication.IdleTimerDisabled = true;
            return base.FinishedLaunching(app, options);
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            // hhx is conversion to unsigned char argument
            // let tokenParts = deviceToken.map { data in String(format: "%02.2hhx", data) }
            //var tokenParts = String.Format(deviceToken
            Logger.LogInfo($"Device Token: {deviceToken.ToHexString()}");
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            Logger.LogError($"Failed to register: {error}");
        }

        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            var timeRemaining = Math.Min(UIApplication.SharedApplication.BackgroundTimeRemaining, 30);
            Logger.LogInfo($"ReceivedRemoteNotification. timeRemaining {timeRemaining}");
            var global = ServiceProvider.GetService<Global>();
            global.HandleRemoteNotification();

            Thread.Sleep(27 * 1000);
            if (application.ApplicationState != UIApplicationState.Active)
                global.OnSleeping();
            // else it'll timeout and system will prevent us from receiving more

        }

        /// <summary>
        ///  Logs the settings the user has _granted_
        /// </summary>
        public static void GetNotificationSettings()
        {
            UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
            {
                Logger.LogInfo($"Notification settings: {settings}");

                if (settings.AuthorizationStatus == UNAuthorizationStatus.Authorized)
                {
                    // must execute on main thread else get runtime warning
                    DispatchQueue.MainQueue.DispatchAsync(new DispatchBlock(() =>
                    UIApplication.SharedApplication.RegisterForRemoteNotifications()));
                }
            });
        }

        private void ConfigureDi(IServiceCollection obj)
        {
            obj.AddSingleton<IHsmStorage, iOSHsmStorage>();
            obj.AddSingleton<INotificationManager, iOSNotificationManager>();
            obj.AddSingleton<iOSNotificationReceiver>();
            obj.AddSingleton<ITorManager, iOSTorManager>();
        }
    }

    internal static class NSDataExtensions
    {
        internal static string ToHexString(this NSData data)
        {
            var bytes = data.ToArray();

            if (bytes == null)
                return null;

            StringBuilder sb = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
                sb.AppendFormat("{0:x2}", b);

            return sb.ToString().ToUpperInvariant();
        }
    }
}
