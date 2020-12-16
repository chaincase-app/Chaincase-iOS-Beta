using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Models;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Logging;
using WalletWasabi.Models;
using WalletWasabi.Wallets;
using Xamarin.Forms;

namespace Chaincase.UI.ViewModels
{
    public class IndexViewModel : ReactiveObject
    {
        protected Global Global { get; }

        private CompositeDisposable Disposables { get; set; }
        private ObservableCollection<TransactionViewModel> _transactions;
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
			{
                Global.SetDefaultWallet();
                Task.Run(async () => await LoadWalletAsync());

                Transactions = new ObservableCollection<TransactionViewModel>();
                TryWriteTableFromCache();
            }
                

            Balance = Global.UiConfig.Balance;

            


            Initializing += OnInit;
            Initializing(this, EventArgs.Empty);

        }

        private event EventHandler Initializing = delegate { };

        private async void OnInit(object sender, EventArgs args)
        {
            Initializing -= OnInit;

            while (Global.Wallet == null || Global.Wallet.State < WalletState.Initialized)
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

                Observable.FromEventPattern(Global.Wallet, nameof(Global.Wallet.NewBlockProcessed))
                    .Merge(Observable.FromEventPattern(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed)))
                    .Throttle(TimeSpan.FromSeconds(3))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(async _ => await TryRewriteTableAsync());

            });
        }

        private void TryWriteTableFromCache()
        {
            try
            {
                var trs = Global.UiConfig.Transactions.Select(ti => new TransactionViewModel(ti));
                Transactions = new ObservableCollection<TransactionViewModel>(trs.OrderByDescending(t => t.DateTime));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private async Task TryRewriteTableAsync()
        {
            try
            {
                var historyBuilder = new TransactionHistoryBuilder(Global.Wallet);
                var txRecordList = await Task.Run(historyBuilder.BuildHistorySummary);
                var tis = txRecordList.Select(txr => new TransactionInfo
                {
                    DateTime = txr.DateTime.ToLocalTime(),
                    Confirmed = txr.Height.Type == HeightType.Chain,
                    Confirmations = txr.Height.Type == HeightType.Chain ? (int)Global.BitcoinStore.SmartHeaderChain.TipHeight - txr.Height.Value + 1 : 0,
                    AmountBtc = $"{txr.Amount.ToString(fplus: true, trimExcessZero: true)}",
                    Label = txr.Label,
                    BlockHeight = txr.Height.Type == HeightType.Chain ? txr.Height.Value : 0,
                    TransactionId = txr.TransactionId.ToString()
                });

                Transactions?.Clear();
                var trs = tis.Select(ti => new TransactionViewModel(ti));

                Transactions = new ObservableCollection<TransactionViewModel>(trs.OrderByDescending(t => t.DateTime));

                Global.UiConfig.Transactions = tis.ToArray();
                Global.UiConfig.ToFile(); // write to file once height is the highest
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
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

        public ObservableCollection<TransactionViewModel> Transactions
        {
            get => _transactions;
            set => this.RaiseAndSetIfChanged(ref _transactions, value);
        }
    }
}
