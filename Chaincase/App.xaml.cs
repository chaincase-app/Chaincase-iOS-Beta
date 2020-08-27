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
using WalletWasabi.Helpers;
using NBitcoin;

namespace Chaincase
{
	public partial class App : Application
	{

		private bool inStartupPhase = true;
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
				.RegisterView<RequestAmountModal, RequestAmountViewModel>()
				.RegisterView<SendAmountPage, SendAmountViewModel>()
				.RegisterView<FeeModal,FeeViewModel>()
				.RegisterView<CoinSelectModal, CoinListViewModel>()
				.RegisterView<CoinDetailModal, CoinViewModel>()
				.RegisterView<SendWhoPage, SendWhoViewModel>()
				.RegisterView<SentPage, SentViewModel>()
				.RegisterView<CoinJoinPage, CoinJoinViewModel>()
				.RegisterView<NewPasswordPage, NewPasswordViewModel>()
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
			Debug.WriteLine("OnStart");
			if (!WalletExists())
			{
				Logger.LogCritical("no wallet");
				//Navigator.NavigateTo(new PassphraseViewModel(Navigator));
			}
		}

		protected override void OnSleep()
		{
			// save app state
			Debug.WriteLine("OnSleep");
			Logger.LogInfo("App entering background state.");
			var mgr = DependencyService.Get<ITorManager>();
			if (mgr?.State != TorState.Stopped)
			{
				mgr.StopAsync();
				var global = Locator.Current.GetService<Global>();
				global.Nodes.Disconnect();


				var synchronizer = Global.Synchronizer;
				if (synchronizer is { })
				{
					synchronizer.StopAsync();
					Logger.LogInfo($"{nameof(Global.Synchronizer)} is stopped.");
				}
			}
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
			Debug.WriteLine("OnResume");
			var mgr = DependencyService.Get<ITorManager>();
			if (mgr?.State != TorState.Started && mgr.State != TorState.Connected)
			{
				mgr.Start(true, GetDataDir());
				var global = Locator.Current.GetService<Global>();
				global.Nodes.Connect();

				var requestInterval = TimeSpan.FromSeconds(30);
				if (Global.Network == Network.RegTest)
				{
					requestInterval = TimeSpan.FromSeconds(5);
				}

				int maxFiltSyncCount = Global.Network == Network.Main ? 1000 : 10000; // On testnet, filters are empty, so it's faster to query them together

				Global.Synchronizer.Start(requestInterval, TimeSpan.FromMinutes(5), maxFiltSyncCount);
				Logger.LogInfo("Start synchronizing filters...");
			}
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
