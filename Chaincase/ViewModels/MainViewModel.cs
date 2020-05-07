using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using Xamarin.Forms;
using Chaincase.Navigation;
using Splat;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.Transactions;
using System.Linq;
using WalletWasabi.Models;
using Chaincase.Models;
using WalletWasabi.Logging;
using NBitcoin;
using System.Threading;

namespace Chaincase.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
        protected Global Global { get; }

        private CompositeDisposable Disposables { get; set; }
        private ObservableCollection<TransactionViewModel> _transactions;
        private StatusViewModel _statusViewModel;
        private CoinListViewModel _coinList;
        public string _balance;
        private string _privateBalance;
        private bool _hasCoins;
        private bool _hasPrivateCoins;
        readonly ObservableAsPropertyHelper<bool> _isJoining;

        public MainViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
        {
            Task.Run(async () => await App.LoadWalletAsync());
            Global = Locator.Current.GetService<Global>();
            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }
            Balance = Global.UiConfig.Balance;

            Transactions = new ObservableCollection<TransactionViewModel>();
            Disposables = new CompositeDisposable();
            StatusViewModel = new StatusViewModel();

            Task.Run(async () =>
            {
                // WaitForInitializationCompletedAsync could be refined to Global.Wallet.Coins/TxProc/Chaumian etc init via Rx
                using var initCts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
                await Global.WaitForInitializationCompletedAsync(initCts.Token).ConfigureAwait(false);

                Observable.FromEventPattern(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed))
                    .Throttle(TimeSpan.FromSeconds(0.1))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        SetBalances();
                    });

                Device.BeginInvokeOnMainThread(() =>
                {
                    CoinList = new CoinListViewModel();

                    Observable.FromEventPattern(Global.Wallet, nameof(Global.Wallet.NewBlockProcessed))
                        .Merge(Observable.FromEventPattern(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed)))
                        .Throttle(TimeSpan.FromSeconds(3))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(async _ => await TryRewriteTableAsync());

                    _ = TryRewriteTableAsync();
                });
            });


            NavReceiveCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                ViewStackService.PushPage(new ReceiveViewModel()).Subscribe();
                return Observable.Return(Unit.Default);
            });

            InitCoinJoin = ReactiveCommand.CreateFromObservable(() =>
            {
                CoinList.SelectOnlyPrivateCoins(false);
                ViewStackService.PushPage(new CoinJoinViewModel(CoinList)).Subscribe();
                return Observable.Return(Unit.Default);
            });

            PrivateSendCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                CoinList.SelectOnlyPrivateCoins(true);
                ViewStackService.PushPage(new SendAmountViewModel(CoinList)).Subscribe();
                return Observable.Return(Unit.Default);
            });

            ExposedSendCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                CoinList.SelectOnlyPrivateCoins(false);
                ViewStackService.PushPage(new SendAmountViewModel(CoinList)).Subscribe();
                return Observable.Return(Unit.Default);
            });


        }

        private async Task TryRewriteTableAsync()
        {
            try
            {
                var historyBuilder = new TransactionHistoryBuilder(Global.Wallet);
                var txRecordList = await Task.Run(historyBuilder.BuildHistorySummary);

                Transactions?.Clear();

                var trs = txRecordList.Select(txr => new TransactionInfo
                {
                    DateTime = txr.DateTime.ToLocalTime(),
                    Confirmed = txr.Height.Type == HeightType.Chain,
                    Confirmations = txr.Height.Type == HeightType.Chain ? (int)Global.BitcoinStore.SmartHeaderChain.TipHeight - txr.Height.Value + 1 : 0,
                    AmountBtc = $"{txr.Amount.ToString(fplus: true, trimExcessZero: true)}",
                    Label = txr.Label,
                    BlockHeight = txr.Height.Type == HeightType.Chain ? txr.Height.Value : 0,
                    TransactionId = txr.TransactionId.ToString()
                }).Select(ti => new TransactionViewModel(ti));

                Transactions = new ObservableCollection<TransactionViewModel>(trs.OrderBy(t => t.DateTime));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private void SetBalances()
		{
            var bal = Global.Wallet.Coins.TotalAmount();
            Balance = bal.ToString();
            Global.UiConfig.Balance = Balance;
            Global.UiConfig.ToFile();
            HasCoins = bal > 0;
            //HasCoins = Balance > 0
            var pbal = GetPrivateBalance();
            PrivateBalance = pbal.ToString();
            HasPrivateCoins = pbal > 0;
        }

        private Money GetPrivateBalance()
        {
            if (Global.Wallet.Coins != null)
            {
                return Enumerable.Where
                (
                    Global.Wallet.Coins,
                    c => c.Unspent && !c.SpentAccordingToBackend && c.AnonymitySet > 1
                ).Sum(c => (long?)c.Amount) ?? 0;
            }
            return 0;
        }

        public bool IsJoining { get { return _isJoining.Value; } }

        public Label Deq;

        public bool HasCoins
        {
            get => _hasCoins;
            set => this.RaiseAndSetIfChanged(ref _hasCoins, value);
        }

        public bool HasPrivateCoins
        {
            get => _hasPrivateCoins;
            set => this.RaiseAndSetIfChanged(ref _hasPrivateCoins, value);
        }

        public StatusViewModel StatusViewModel
        {
            get => _statusViewModel;
            set => this.RaiseAndSetIfChanged(ref _statusViewModel, value);
        }

        public CoinListViewModel CoinList
        {
            get => _coinList;
            set => this.RaiseAndSetIfChanged(ref _coinList, value);
        }

        public string Balance
        {
            get => _balance;
            set => this.RaiseAndSetIfChanged(ref _balance, value);
        }

        public string PrivateBalance
        {
            get => _privateBalance;
            set => this.RaiseAndSetIfChanged(ref _privateBalance, value);
        }

        public ObservableCollection<TransactionViewModel> Transactions
        {
            get => _transactions;
            set => this.RaiseAndSetIfChanged(ref _transactions, value);
        }

        public ReactiveCommand<Unit, Unit> NavReceiveCommand;
		public ReactiveCommand<Unit, Unit> ExposedSendCommand;
        public ReactiveCommand<Unit, Unit> PrivateSendCommand;
        public ReactiveCommand<Unit, Unit> InitCoinJoin;
    }
}
