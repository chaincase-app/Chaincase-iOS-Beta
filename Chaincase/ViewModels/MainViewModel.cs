using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using Chaincase.Controllers;
using Xamarin.Forms;
using Chaincase.Navigation;
using Splat;

namespace Chaincase.ViewModels
{
	public class MainViewModel : BaseViewModel
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

        public MainViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
        {
            SetBalance();

            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }

            Disposables = new CompositeDisposable();
            //CoinList = new CoinListViewModel();

            //NavReceiveCommand = ReactiveCommand.CreateFromObservable(() =>
            //{
            //    ViewStackService.PushPage(new ReceiveViewModel(viewStackService)).Subscribe();
            //    return Observable.Return(Unit.Default);
            //});

            //NavSendCommand = ReactiveCommand.CreateFromObservable(() =>
            //{
            //    ViewStackService.Router.Navigate.Execute(new SendAmountViewModel(viewStackService, CoinList)).Subscribe();
            //    return Observable.Return(Unit.Default);
            //});

            //InitCoinJoin = ReactiveCommand.CreateFromObservable(() =>
            //{
            //    ViewStackService.Router.Navigate.Execute(new CoinJoinViewModel(viewStackService, CoinList)).Subscribe();
            //    return Observable.Return(Unit.Default);
            //});

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
