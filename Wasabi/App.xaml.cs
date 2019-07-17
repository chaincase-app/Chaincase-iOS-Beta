using System;
using System.IO;
using System.Threading.Tasks;
using WalletWasabi.Logging;
using Wasabi.Navigation;
using Wasabi.Views;
using Xamarin.Forms;

namespace Wasabi
{
	public partial class App : Application
	{
		public static INavigationService NavigationService { get; } = new NavigationService();

		public App()
		{
			InitializeComponent();

			//NavigationService.Configure("MainPage", typeof(Views.MainPage));
			NavigationService.Configure("PassphrasePage", typeof(Views.PassphrasePage));
			NavigationService.Configure("MnemonicPage", typeof(Views.MnemonicPage));
			NavigationService.Configure("VerifyMnemonicPage", typeof(Views.VerifyMnemonicPage));
			NavigationService.Configure("ReceivePage", typeof(Views.ReceivePage));
			var mainPage = ((NavigationService)NavigationService).SetRootPage("PassphrasePage");

			MainPage = mainPage;
		}

		protected override void OnStart()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			Logger.InitializeDefaults(Path.Combine(Global.DataDir, "Logs.txt"));
			Global.InitializeNoWalletAsync();
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}

		private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			Logger.LogWarning(e?.Exception, "UnobservedTaskException");
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Logger.LogWarning(e?.ExceptionObject as Exception, "UnhandledException");
		}
	}
}
