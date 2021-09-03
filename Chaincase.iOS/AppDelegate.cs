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
using WalletWasabi.Wallets;

namespace Chaincase.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        private Global _global;
        private NBitcoin.Network _network;
        private APNSEnrollmentClient _apnsEnrollmentClient;

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
            _global = formsApp.ServiceProvider.GetService<Global>();
            _network = formsApp.ServiceProvider.GetService<Config>().Network;
            _apnsEnrollmentClient = formsApp.ServiceProvider.GetService<APNSEnrollmentClient>();

            MessagingCenter.Subscribe<InitializeNoWalletTaskMessage>(this, "InitializeNoWalletTaskMessage", async message =>
            {
                var context = new iOSInitializeNoWalletContext(_global);
                await context.InitializeNoWallet();
            });

            MessagingCenter.Subscribe<OnSleepingTaskMessage>(this, "OnSleepingTaskMessage", async message =>
            {
                var context = new iOSOnSleepingContext(_global);
                await context.OnSleeping();
            });

            formsApp.InitializeNoWallet();

            UNUserNotificationCenter.Current.Delegate =
                formsApp.ServiceProvider.GetService<iOSNotificationReceiver>();
            RegisterForRemoteNotifications(); // if permission already granted
            LoadApplication(formsApp);
            UIApplication.SharedApplication.IdleTimerDisabled = true;
            return base.FinishedLaunching(app, options);
        }

        public override async void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
#if DEBUG
                var isDebug = true;
#else
                var isDebug = false;
#endif
            Logger.LogInfo($"Registered Remote Notifications Device Token: {deviceToken.ToHexString()} isDebug: {isDebug}");
            try
            {
                await _apnsEnrollmentClient.StoreTokenAsync(deviceToken.ToHexString(), isDebug);
            }
            catch
            {
                Logger.LogInfo($"Failed to store token on backend");
            }
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            Logger.LogError($"Failed to register: {error}");
        }

        // This method is only called in the background
        // When the app is in foreground and receives a remote notification
        // it arrives at userNotificationCenter
        public override async void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            _global.HandleRemoteNotification();

            var timeRemaining = Math.Min(UIApplication.SharedApplication.BackgroundTimeRemaining, 30);
            await Task.Delay(((int)timeRemaining-1) * 1000); // sleeping takes 100ms, 1s allowance > sufficient
            Logger.LogInfo($"ReceivedRemoteNotification. timeRemaining {timeRemaining}");
            if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
                await _global.OnSleeping();
            // else it'll timeout and system will prevent us from receiving more

        }

        /// <summary>
        ///  Logs the settings the user has _granted_
        /// </summary>
        public static void RegisterForRemoteNotifications()
        {
            UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
            {
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
            obj.AddSingleton<WalletDirectories, iOSWalletDirectories>();
            obj.AddTransient<APNSEnrollmentClient>();
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
