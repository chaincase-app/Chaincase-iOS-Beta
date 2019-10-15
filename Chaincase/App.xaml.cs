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

namespace Chaincase
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			Logger.InitializeDefaults(Path.Combine(Global.DataDir, "Logs.txt"));
			Task.Run(async () => { await Global.InitializeNoWalletAsync(); }).Wait();
			WalletController.LoadWalletAsync(Global.Network);

			var bs = new AppBootstrapper();
			MainPage = bs.CreateMainPage();
		}

		protected override void OnStart()
		{


			/*
			if (!WalletController.WalletExists(Global.Network))
			{
				System.Diagnostics.Debug.WriteLine("no wallet");
				Navigator.NavigateTo(new PassphraseViewModel(Navigator));
			}
			*/
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
