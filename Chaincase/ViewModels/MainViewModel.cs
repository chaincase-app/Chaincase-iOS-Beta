using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using Chaincase.Controllers;
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

namespace Chaincase.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
		private CompositeDisposable Disposables { get; set; }
        private ObservableCollection<TransactionViewModel> _transactions;
        private StatusViewModel _statusViewModel;
        private CoinListViewModel _coinList;
        private string _balance;
        private String _privateBalance;
        private bool _hasCoins;
        private bool _hasPrivateCoins;
        readonly ObservableAsPropertyHelper<bool> _isJoining;

        public MainViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
        {
            SetBalances();

            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }

            Disposables = new CompositeDisposable();
            StatusViewModel = new StatusViewModel(Global.Nodes.ConnectedNodes, Global.Synchronizer);

            CoinList = new CoinListViewModel();

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

            Observable.FromEventPattern(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed))
				.Merge(Observable.FromEventPattern(Global.Wallet.ChaumianClient, nameof(Global.Wallet.ChaumianClient.OnDequeue)))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(o => {
                    SetBalances();
                })
				.DisposeWith(Disposables);

            Transactions = new ObservableCollection<TransactionViewModel>();

            Observable.FromEventPattern(Global.Wallet, nameof(Global.Wallet.NewBlockProcessed))
                .Merge(Observable.FromEventPattern(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed)))
                .Throttle(TimeSpan.FromSeconds(3))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await TryRewriteTableAsync())
                .DisposeWith(Disposables);

            _ = TryRewriteTableAsync();
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
            var bal = WalletController.GetBalance(Global.Network);
			Balance = bal.ToString();
            HasCoins = bal > 0;

            var pbal = WalletController.GetPrivateBalance(Global.Network);
            PrivateBalance = pbal.ToString();
            HasPrivateCoins = pbal > 0;
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
