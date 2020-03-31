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

		public App()
		{
			InitializeComponent();

			// Probably asyncify and add everything after "Logger" above here
			// TODO Invert this so UI is created FIRST then app internals
			Logger.InitializeDefaults(Path.Combine(Global.DataDir, "Logs.txt"));
			Task.Run(async () => { await Global.InitializeNoWalletAsync(); }).Wait();
			var walletExists = WalletController.WalletExists(Global.Network);
			if (walletExists) WalletController.LoadWalletAsync(Global.Network);

			Locator
				.CurrentMutable
                .RegisterView<MainPage, MainViewModel>()
                .RegisterView<LandingPage, LandingViewModel>()
                .RegisterView<ReceivePage, ReceiveViewModel>()
                .RegisterView<AddressPage, AddressViewModel>()
                .RegisterView<SendAmountPage, SendAmountViewModel>()
				.RegisterView<SendWhoPage, SendWhoViewModel>()
                .RegisterView<SentPage, SentViewModel>()
				.RegisterView<CoinJoinPage, CoinJoinViewModel>()
                .RegisterView<PassphrasePage, PassphraseViewModel>()
                .RegisterView<MnemonicPage, MnemonicViewModel>()
                .RegisterView<VerifyMnemonicPage, VerifyMnemonicViewModel>()
                .RegisterView<PasswordPromptModal, PasswordPromptViewModel>()
                .RegisterNavigationView(() => new NavigationView());

			var page = walletExists ? (IViewModel)new MainViewModel() : new LandingViewModel();

			Locator
				.Current
				.GetService<IViewStackService>()
				.PushPage(page, null, true, false)
				.Subscribe();

			MainPage = Locator.Current.GetNavigationView();

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			
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
