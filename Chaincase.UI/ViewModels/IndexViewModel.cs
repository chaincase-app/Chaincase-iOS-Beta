using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Common;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Logging;
using WalletWasabi.Wallets;
using Xamarin.Forms;

namespace Chaincase.UI.ViewModels
{
    public class IndexViewModel : ReactiveObject
    {
        protected Global Global { get; }

        private CompositeDisposable Disposables { get; set; }
        public string _balance;
        private ObservableAsPropertyHelper<bool> _hasCoins;
        private ObservableAsPropertyHelper<bool> _hasSeed;
        private ObservableAsPropertyHelper<bool> _isBackedUp;
        private ObservableAsPropertyHelper<bool> _canBackUp;
        private bool _hasPrivateCoins;
        readonly ObservableAsPropertyHelper<bool> _isJoining;

        public IndexViewModel()
        {
            Global = Locator.Current.GetService<Global>();
            if (Global.WalletExists())
                Global.SetDefaultWallet();
                Task.Run(async () => await LoadWalletAsync());

            // init with UI config
            Balance = Global.UiConfig.Balance;

            Initializing += OnInit;
            Initializing(this, EventArgs.Empty);

        }

        private event EventHandler Initializing = delegate { };

        private async void OnInit(object sender, EventArgs args)
        {
            Initializing -= OnInit;

            while (Global.Wallet.State < WalletState.Initialized)
            {
                await Task.Delay(200);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                //CoinList = new CoinListViewModel();
                Observable.FromEventPattern(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed))
                   .Throttle(TimeSpan.FromSeconds(0.1))
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(_ =>
                   {
                       // TODO make ObservableAsPropertyHelper
                       Balance = Global.Wallet.Coins.TotalAmount().ToString();
                   });

            });
        }

        private static async Task LoadWalletAsync()
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

        public string Balance
        {
            get => _balance;
            set => this.RaiseAndSetIfChanged(ref _balance, value);
        }
    }
}
