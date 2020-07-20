using System;
using System.IO;
using System.Threading.Tasks;
using WalletWasabi.Logging;
using Chaincase.Navigation;
using Chaincase.Views;
using Xamarin.Forms;
using System.Diagnostics;
using Chaincase.ViewModels;
using Splat;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase
{
	public partial class App : Application
	{

		private static Global Global;

		public App()
		{
			InitializeComponent();

			Global = new Global();
			Task.Run(async () => { await Global.InitializeNoWalletAsync(); }); // this is the only thing that takes forever
			Locator.CurrentMutable.RegisterConstant(Global);

			Locator
				.CurrentMutable
				.RegisterView<MainPage, MainViewModel>()
				.RegisterView<WalletInfoPage, WalletInfoViewModel>()
				.RegisterView<TransactionDetailPage, TransactionViewModel>()
				.RegisterView<LandingPage, LandingViewModel>()
				.RegisterView<LoadWalletPage, LoadWalletViewModel>()
				.RegisterView<ReceivePage, ReceiveViewModel>()
				.RegisterView<AddressPage, AddressViewModel>()
				.RegisterView<SendAmountPage, SendAmountViewModel>()
				.RegisterView<FeeModal,FeeViewModel>()
				.RegisterView<CoinSelectModal, CoinListViewModel>()
				.RegisterView<CoinDetailModal, CoinViewModel>()
				.RegisterView<SendWhoPage, SendWhoViewModel>()
				.RegisterView<SentPage, SentViewModel>()
				.RegisterView<CoinJoinPage, CoinJoinViewModel>()
				.RegisterView<NewPasswordPage, NewPasswordViewModel>()
				.RegisterView<MnemonicPage, MnemonicViewModel>()
				.RegisterView<VerifyMnemonicPage, VerifyMnemonicViewModel>()
				.RegisterView<PasswordPromptModal, PasswordPromptViewModel>()
				.RegisterView<StartBackUpModal, StartBackUpViewModel>()
				.RegisterView<BackUpModal, BackUpViewModel>()
				.RegisterNavigationView(() => new NavigationView());

			var page = WalletExists() ? (IViewModel)new MainViewModel() : new LandingViewModel();

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
			if (!WalletExists())
			{
				Logger.LogCritical("no wallet");
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

		public static async Task LoadWalletAsync()
		{
			string walletName = Global.Network.ToString();
			KeyManager keyManager = Global.WalletManager.GetWalletByName(walletName).KeyManager;
			if (keyManager is null)
			{
				return;
			}

			try
			{
				Global.Wallet = await Global.WalletManager.StartWalletAsync(keyManager);
				// Successfully initialized.
			}
			catch (OperationCanceledException ex)
			{
				Logger.LogTrace(ex);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
			}
		}

		private bool WalletExists()
		{
			var walletName = Global.Network.ToString();
			(string walletFullPath, _) = Global.WalletManager.WalletDirectories.GetWalletFilePaths(walletName);
			return File.Exists(walletFullPath);
		}
	}
}
