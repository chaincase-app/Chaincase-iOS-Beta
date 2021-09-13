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
using System.IO;

namespace Chaincase.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        private Global _global;
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
            var dataDir = formsApp.ServiceProvider.GetService<IDataDirProvider>().Get();
            SetLoggerPermissions(dataDir);
            _global = formsApp.ServiceProvider.GetService<Global>();
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
        public override async void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            Logger.LogInfo($"DidReceiveRemoteNotification");
            _global.HandleRemoteNotification();

            await Task.Delay(29_000); // sleeping takes 100ms, 1s allowance > sufficient
            if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
                using (BenchmarkLogger.Measure(operationName: "Sleeping after Push"))
                {
                    await _global.OnSleeping().ConfigureAwait(false);
                };
            // else it'll timeout and system will prevent us from receiving more
            completionHandler.Invoke(UIBackgroundFetchResult.NewData);
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

        private bool SetLoggerPermissions(string dataDir)
        {
            return NSFileManager.DefaultManager.SetAttributes(
                new NSFileAttributes()
                {
                    ProtectionKey = NSFileProtection.CompleteUntilFirstUserAuthentication
                },
                Path.Combine(dataDir, "Logs.txt"),
                out NSError e);
            if (e != null) // faux catch accomodates bound obj-c)
            {
                Logger.LogWarning(e.LocalizedDescription);
            }
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
