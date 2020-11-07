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
using WalletWasabi.Wallets;

namespace Chaincase.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
        protected Global Global { get; }

        private CompositeDisposable Disposables { get; set; }
        private ObservableCollection<TransactionViewModel> _transactions;
        private StatusViewModel _statusViewModel;
        private CoinListViewModel _coinList;
        private CoinJoinViewModel _coinJoinViewModel;
        private SendAmountViewModel _sendAmountViewModel;
        public string _balance;
        private ObservableAsPropertyHelper<bool> _hasCoins;
        private ObservableAsPropertyHelper<bool> _hasSeed;
        private ObservableAsPropertyHelper<bool> _isBackedUp;
        private ObservableAsPropertyHelper<bool> _canBackUp;
        private bool _hasPrivateCoins;
        readonly ObservableAsPropertyHelper<bool> _isJoining;

        public MainViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
        {
            Global = Locator.Current.GetService<Global>();
            Global.SetDefaultWallet();
            //Task.Run(async () => await App.LoadWalletAsync());

            ShowWalletInfoCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                ViewStackService.PushPage(new WalletInfoViewModel(Global.Wallet.KeyManager)).Subscribe();
                return Observable.Return(Unit.Default);
            });

            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }

            // init with UI config
            Balance = Global.UiConfig.Balance;

            Transactions = new ObservableCollection<TransactionViewModel>();

            OpenTransactionDetail = ReactiveCommand.CreateFromObservable((TransactionViewModel tvm) =>
            {
                ViewStackService.PushPage(tvm).Subscribe();
                return Observable.Return(Unit.Default);
            });

            TryWriteTableFromCache();

            Initializing += OnInit;
            Initializing(this, EventArgs.Empty);

            StatusViewModel = new StatusViewModel();

            var coinListReady = this.WhenAnyValue(x => x.CoinList.IsCoinListLoading,
                stillLoading => !stillLoading);

            _hasSeed = this.WhenAnyValue(x => x.Global.UiConfig.HasSeed)
                .ToProperty(this, nameof(HasSeed));

            _isBackedUp = this.WhenAnyValue(x => x.Global.UiConfig.IsBackedUp)
                .ToProperty(this, nameof(IsBackedUp));

            var canBackUp = this.WhenAnyValue(x => x.HasSeed, x => x.IsBackedUp,
               (hasSeed, isBackedUp) => hasSeed && !isBackedUp);

            canBackUp.ToProperty(this, x => x.CanBackUp, out _canBackUp);

            NavBackUpCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                ViewStackService.PushPage(new StartBackUpViewModel()).Subscribe();
                return Observable.Return(Unit.Default);
            }, canBackUp);

            NavReceiveCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                ViewStackService.PushPage(new ReceiveViewModel()).Subscribe();
                return Observable.Return(Unit.Default);
            });

            InitCoinJoin = ReactiveCommand.CreateFromObservable(() =>
            {
                CoinList.SelectOnlyPrivateCoins(false);
                ViewStackService.PushPage(CoinJoinViewModel).Subscribe();
                return Observable.Return(Unit.Default);
            }, coinListReady);

            SendCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                CoinList.SelectOnlyPrivateCoins(true);
                ViewStackService.PushPage(SendAmountViewModel).Subscribe();
                return Observable.Return(Unit.Default);
            }, coinListReady);

            _hasCoins = this
                .WhenAnyValue(x => x.Balance)
                .Select(bal => Money.Parse(bal) > 0)
                .ToProperty(this, nameof(HasCoins));           
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
                CoinList = new CoinListViewModel();
                Observable.FromEventPattern(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed))
                   .Throttle(TimeSpan.FromSeconds(0.1))
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(_ =>
                   {
                       Balance = Global.Wallet.Coins.TotalAmount().ToString();
                       HasPrivateCoins = Enumerable.Where(
                               Global.Wallet.Coins,
                               c => c.Unspent && !c.SpentAccordingToBackend && c.AnonymitySet > 1
                           ).Sum(c => (long?)c.Amount) > 0;
                   });

                Observable.FromEventPattern(Global.Wallet, nameof(Global.Wallet.NewBlockProcessed))
                    .Merge(Observable.FromEventPattern(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed)))
                    .Throttle(TimeSpan.FromSeconds(3))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(async _ => await TryRewriteTableAsync());

                CoinJoinViewModel = new CoinJoinViewModel(CoinList);
                SendAmountViewModel = new SendAmountViewModel(CoinList);
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

        public bool IsJoining { get { return _isJoining.Value; } }

        public Label Deq;

        public bool IsBackedUp => _isBackedUp.Value;

        public bool HasSeed => _hasSeed.Value;

        public bool CanBackUp => _canBackUp.Value;

        public bool HasCoins => _hasCoins.Value;

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

        public CoinJoinViewModel CoinJoinViewModel
        {
            get => _coinJoinViewModel;
            set => this.RaiseAndSetIfChanged(ref _coinJoinViewModel, value);
        }

        public SendAmountViewModel SendAmountViewModel
        {
            get => _sendAmountViewModel;
            set => this.RaiseAndSetIfChanged(ref _sendAmountViewModel, value);
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

        public ReactiveCommand<Unit, Unit> ShowWalletInfoCommand;
        public ReactiveCommand<Unit, Unit> NavBackUpCommand;
        public ReactiveCommand<Unit, Unit> NavReceiveCommand;
		public ReactiveCommand<Unit, Unit> ExposedSendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand;
        public ReactiveCommand<Unit, Unit> InitCoinJoin;
        public ReactiveCommand<TransactionViewModel, Unit> OpenTransactionDetail;

    }
}
