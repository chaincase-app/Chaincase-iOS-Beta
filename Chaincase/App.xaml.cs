using System;
using System.IO;
using System.Threading.Tasks;
using WalletWasabi.Logging;
using Chaincase.Navigation;
using Chaincase.Views;
using Chaincase.Controllers;
using Xamarin.Forms;
using System.Diagnostics;
using Chaincase.ViewModels;
using ReactiveUI;
using Splat;

namespace Chaincase
{
	public partial class App : Application
	{
		private static App instance;
        private readonly Func<MainView> _mainView;

		public App(Func<MainView> mainView)
		{
			instance = this;
			InitializeComponent();
			_mainView = mainView;

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			
		}

        // Probably asyncify and add everything after "Logger" above here
        // TODO Invert this so UI is created FIRST then app internals
        public void Initialize()
		{
			Logger.InitializeDefaults(Path.Combine(Global.DataDir, "Logs.txt"));
			Task.Run(async () => { await Global.InitializeNoWalletAsync(); }).Wait();
			var walletExists = WalletController.WalletExists(Global.Network);
			if (walletExists) WalletController.LoadWalletAsync(Global.Network);

            MainPage = _mainView();
		}

		protected override void OnStart()
		{



			if (!WalletController.WalletExists(Global.Network))
			{
				System.Diagnostics.Debug.WriteLine("no wallet");
				//Navigator.NavigateTo(new PassphraseViewModel(Navigator));
			}

		}

		protected override void OnSleep()
		{
			Debug.WriteLine("OnSleep");
			// Task.Run(async () => { await Global.DisposeAsync(); }).Wait();
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
