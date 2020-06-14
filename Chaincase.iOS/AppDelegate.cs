using System;
using System.IO;
using Foundation;
using Splat;
using UIKit;
using UserNotifications;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using Xamarin.Forms;

namespace Chaincase.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		private bool inStartupPhase = true;
		//
		// This method is invoked when the application has loaded and is ready to run. In this 
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching(UIApplication application, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init();

			UNUserNotificationCenter.Current.Delegate = new iOSNotificationReceiver();
			ZXing.Net.Mobile.Forms.iOS.Platform.Init();
			LoadApplication(new App());

			return base.FinishedLaunching(application, options);
		}

		public override void OnActivated(UIApplication application)
		{
			Logger.LogInfo("OnActivated called, App did become active.");
			var mgr = DependencyService.Get<ITorManager>();
			if (!inStartupPhase && mgr?.State != TorState.Started && mgr.State != TorState.Connected) {
				mgr.Start(true, GetDataDir());
				var global = Locator.Current.GetService<Global>();
				global.Nodes.Connect();
			} else inStartupPhase = false;
		}

		public override void DidEnterBackground(UIApplication application)
		{
			Logger.LogInfo("App entering background state.");
			var mgr = DependencyService.Get<ITorManager>();
			if (mgr?.State != TorState.Stopped) {
				mgr.StopAsync();
				var global = Locator.Current.GetService<Global>();
				global.Nodes.Disconnect();
			}

		}

		public override void WillEnterForeground(UIApplication application)
		{
			Logger.LogInfo("App will enter foreground");
		}
		public override void OnResignActivation(UIApplication application)
		{
			Logger.LogInfo("OnResignActivation called, App moving to inactive state.");
		}
		// not guaranteed that this will run
		public override void WillTerminate(UIApplication application)
		{
			Logger.LogInfo("App is terminating.");
		}

		private string GetDataDir()
		{
			string dataDir;
			if (Device.RuntimePlatform == Device.iOS)
			{
				var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				var library = Path.Combine(documents, "..", "Library", "Client");
				dataDir = library;
			}
			else
			{
				dataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Chaincase", "Client"));
			}
			return dataDir;
		}
	}
}
