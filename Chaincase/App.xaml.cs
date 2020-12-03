using System;
using System.IO;
using System.Threading.Tasks;
using Chaincase.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Chaincase.Navigation;
using Chaincase.ViewModels;
using Chaincase.Views;
using Microsoft.MobileBlazorBindings;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using Xamarin.Forms;

namespace Chaincase
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public IServiceProvider Container => _host.Services;

        public App(Action<IServiceCollection> configureOSServices)
        {
            InitializeComponent();

            var hostBuilder = MobileBlazorBindingsHost.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddBlazorHybrid();
                    services.UseMicrosoftDependencyResolver();
                    var resolver = Locator.CurrentMutable;
                    resolver.InitializeSplat();
                    resolver.InitializeReactiveUI();

                    // shorthand for e.g.
                    // services.AddTransient<IViewFor<SecondaryViewModel>, SecondaryPage>();

                    resolver
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

                    configureOSServices?.Invoke(services);

                    services.AddSingleton<Global, Global>();
                })
                .UseWebRoot("wwwroot");

            hostBuilder.UseStaticFiles();
            

            _host = hostBuilder.Build();
            Container.UseMicrosoftDependencyResolver();

            // This relies on Global registered
            var message = new InitializeNoWalletTaskMessage();
            MessagingCenter.Send(message, "InitializeNoWalletTaskMessage");

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

        protected override void OnSleep()
        {
            // Execute Sleeping Background Task
            var message = new OnSleepingTaskMessage();
            MessagingCenter.Send(message, "OnSleepingTaskMessage");
        }

        private event EventHandler Resuming = delegate { };

        protected override void OnResume()
        {
            Resuming += OnResuming;
            // Execute Async code
            Resuming(this, EventArgs.Empty);
        }

        private async void OnResuming(object sender, EventArgs args)
        {
            //unsubscribe from event
            Resuming -= OnResuming;

            //perform non-blocking actions
            await Locator.Current.GetService<Global>().OnResuming();
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
            var global = Locator.Current.GetService<Global>();
            var walletName = global.Network.ToString();
            (string walletFullPath, _) = global.WalletManager.WalletDirectories.GetWalletFilePaths(walletName);
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

    // classes to communicate between Libs & specific platforms via messaging center

    public class StartLongRunningTaskMessage { }

    public class StopLongRunningTaskMessage { }
}
