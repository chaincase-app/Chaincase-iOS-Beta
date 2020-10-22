using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Chaincase.Navigation;
using Chaincase.ViewModels;
using Chaincase.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.MobileBlazorBindings;
using Splat;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using Xamarin.Forms;

namespace Chaincase
{
    public partial class App : Application
    {
	    public Global Global { get; }
			
		public App(Action<IServiceCollection> configureDI)
        {
			InitializeComponent();
			//BlazorHybridHost.AddResourceAssembly(GetType().Assembly, contentRoot: "WebUI/wwwroot");

			var host = MobileBlazorBindingsHost.CreateDefaultBuilder()
				.ConfigureServices((hostContext, services) =>
				{
					// Adds web-specific services such as NavigationManager
					services.AddBlazorHybrid();

			//		// Register app-specific services
			//		services.AddSingleton<CounterState>();
			//		services.AddSingleton<AppStateService>();
					configureDI?.Invoke(services);
				})
				.Build();

			Global = new Global(host);
			
			Task.Run(async () =>
			{
				try
				{
					await Global.InitializeNoWalletAsync().ConfigureAwait(false);
				}
				catch (OperationCanceledException ex)
				{
					Logger.LogTrace(ex);
				}
			});

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

            //host.AddComponent<Main>(parent: MainPage);
        }

		private event EventHandler Sleeping = delegate { };

		protected override void OnSleep()
		{
			Debug.WriteLine("OnSleep");
			Sleeping += OnSleeping;
			// Execute Async code
			Sleeping(this, EventArgs.Empty);
		}

		private async void OnSleeping(object sender, EventArgs args)
		{
			//unsubscribe from event
			Sleeping -= OnSleeping;

			//perform non-blocking actions
			await Global.OnSleeping();
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
			await Global.OnResuming();
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
			var global = Locator.Current.GetService<Global>();
			string walletName = global.Network.ToString();
			KeyManager keyManager = global.WalletManager.GetWalletByName(walletName).KeyManager;
			if (keyManager is null)
			{
				return;
			}

			try
			{
				global.Wallet = await global.WalletManager.StartWalletAsync(keyManager);
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
