using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using Chaincase.Controllers;
using Xamarin.Forms;

namespace Chaincase.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
		private CompositeDisposable Disposables { get; set; }
        private CoinListViewModel _coinList;
        public CoinListViewModel CoinList
        {
            get => _coinList;
            set => this.RaiseAndSetIfChanged(ref _coinList, value);
        }

        private String _balance;
		public String Balance
		{
			get => _balance;
			set => this.RaiseAndSetIfChanged(ref _balance, value);
		}

        public ReactiveCommand<Unit, Unit> NavReceiveCommand;
		public ReactiveCommand<Unit, Unit> NavSendCommand;

        public ReactiveCommand<Unit, Unit> InitCoinJoin { get; private set; }
        readonly ObservableAsPropertyHelper<bool> _isJoining;
        public bool IsJoining { get { return _isJoining.Value; } }

        public Label Deq;

        public MainViewModel(IScreen hostScreen) : base(hostScreen)
        {
            SetBalance();

            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }

            Disposables = new CompositeDisposable();
            CoinList = new CoinListViewModel(hostScreen);

            NavReceiveCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                HostScreen.Router.Navigate.Execute(new ReceiveViewModel(hostScreen)).Subscribe();
                return Observable.Return(Unit.Default);
            });

            NavSendCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                HostScreen.Router.Navigate.Execute(new SendAmountViewModel(hostScreen, CoinList)).Subscribe();
                return Observable.Return(Unit.Default);
            });

            InitCoinJoin = ReactiveCommand.CreateFromObservable(() =>
            {
                HostScreen.Router.Navigate.Execute(new CoinJoinViewModel(hostScreen, CoinList)).Subscribe();
                return Observable.Return(Unit.Default);
            });

            Observable.FromEventPattern(Global.WalletService.TransactionProcessor, nameof(Global.WalletService.TransactionProcessor.WalletRelevantTransactionProcessed))
				.Merge(Observable.FromEventPattern(Global.ChaumianClient, nameof(Global.ChaumianClient.OnDequeue)))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(o => SetBalance())
				.DisposeWith(Disposables);
		}

		private void SetBalance()
		{
			Balance = WalletController.GetBalance().ToString();
		}
    }
}
