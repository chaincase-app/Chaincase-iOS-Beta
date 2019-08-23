using System;
using System.IO;
using System.Threading.Tasks;
using WalletWasabi.Logging;
using Wasabi.Navigation;
using Wasabi.Views;
using Wasabi.Controllers;
using Xamarin.Forms;
using System.Diagnostics;
using Wasabi.ViewModels;

namespace Wasabi
{
	public partial class App : Application, IHaveMainPage
	{
		public static INavigationService Navigator { get; private set; }

		public App()
		{
			InitializeComponent();
			Navigator = new NavigationService(this, new ViewLocator());
			//NavigationService.Configure("MainPage", typeof(Views.MainPage));
			//NavigationService.Configure("PassphrasePage", typeof(PassphrasePage));
			//NavigationService.Configure("MnemonicPage", typeof(MnemonicPage));
			//NavigationService.Configure("VerifyMnemonicPage", typeof(VerifyMnemonicPage));
			//NavigationService.Configure("MainPage", typeof(MainPage));
			//NavigationService.Configure("ReceivePage", typeof(ReceivePage));
			//NavigationService.Configure("CoinListPage", typeof(CoinListPage));
		}

		protected override void OnStart()
		{

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			Logger.InitializeDefaults(Path.Combine(Global.DataDir, "Logs.txt"));
			Task.Run(async () => { await Global.InitializeNoWalletAsync(); }).Wait();
			WalletController.LoadWalletAsync(Global.Network);
			var rootViewModel = new MainViewModel(Navigator);
			Navigator.PresentAsNavigatableMainPage(rootViewModel);

			if (!WalletController.WalletExists(Global.Network))
			{
				System.Diagnostics.Debug.WriteLine("no wallet");
				Navigator.NavigateTo(new PassphraseViewModel(Navigator));
			}
		}

		protected override void OnSleep()
		{
			Debug.WriteLine("OnSleep");
			Task.Run(async () => { await Global.DisposeAsync(); }).Wait();
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
