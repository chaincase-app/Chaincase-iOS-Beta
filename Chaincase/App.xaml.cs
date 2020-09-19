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
using NBitcoin.Protocol.Behaviors;
using System.Linq;
using NBitcoin.Protocol;

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
			Task.Run(async () => { await Global.InitializeNoWalletAsync().ConfigureAwait(false); });
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
				.RegisterView<FeeModal, FeeViewModel>()
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

		private event EventHandler Sleeping = delegate { };

		protected override void OnSleep()
		{
			Debug.WriteLine("OnSleep");
			Sleeping += OnSleeping;
			Sleeping(this, EventArgs.Empty);			
		}

		private async void OnSleeping(object sender, EventArgs args)
		{
			Sleeping -= OnSleeping;
			var mgr = DependencyService.Get<ITorManager>();
			if (mgr?.State != TorState.Stopped)
			{
				var global = Locator.Current.GetService<Global>();

				var synchronizer = Global.Synchronizer;
				if (synchronizer is { })
				{
					await synchronizer.StopAsync();
					Logger.LogInfo($"{nameof(Global.Synchronizer)} is stopped.");
				}

				var addressManagerFilePath = global.AddressManagerFilePath;
				if (addressManagerFilePath is { })
				{
					IoHelpers.EnsureContainingDirectoryExists(addressManagerFilePath);
					var addressManager = global.AddressManager;
					if (addressManager is { })
					{
						addressManager.SavePeerFile(global.AddressManagerFilePath, global.Config.Network);
						Logger.LogInfo($"{nameof(AddressManager)} is saved to `{global.AddressManagerFilePath}`.");
					}
				}

				await mgr.StopAsync();
				global.Nodes.Disconnect();
			}
		}

		private event EventHandler Resuming = delegate { };

		protected override void OnResume()
		{
			Debug.WriteLine("OnResume");
			Resuming += OnResuming;
			// Execute Async code
			Resuming(this, EventArgs.Empty);
		}

		private async void OnResuming(object sender, EventArgs args)
		{
			//unsubscribe from event
			Resuming -= OnResuming;
			//perform non-blocking actions
			var mgr = DependencyService.Get<ITorManager>();
			if (mgr?.State != TorState.Started && mgr.State != TorState.Connected)
			{
				var global = Locator.Current.GetService<Global>();

				var userAgent = Constants.UserAgents.RandomElement();
				var connectionParameters = new NodeConnectionParameters { UserAgent = userAgent };
				var addrManTask = global.InitializeAddressManagerBehaviorAsync();
				AddressManagerBehavior addressManagerBehavior = await addrManTask.ConfigureAwait(false);
				connectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

				mgr.Start(false, GetDataDir());
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
