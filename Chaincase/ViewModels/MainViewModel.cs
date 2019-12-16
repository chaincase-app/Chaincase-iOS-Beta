using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Models;
using WalletWasabi.Logging;
using Chaincase.Controllers;
using Chaincase.Navigation;
using Xamarin.Forms;
using Splat;

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

        public ReactiveCommand<Unit, Unit> CoinJoin { get; private set; }
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
                HostScreen.Router.Navigate.Execute(new SendAmountViewModel(hostScreen)).Subscribe();
                return Observable.Return(Unit.Default);
            });

            CoinJoin = ReactiveCommand.CreateFromObservable(CoinJoinImpl);
            CoinJoin.IsExecuting.ToProperty(this, x => x.IsJoining, out _isJoining);
            CoinJoin.ThrownExceptions.Subscribe(ex => Logger.LogError<MainViewModel>(ex));

            Observable.FromEventPattern(Global.WalletService.Coins, nameof(Global.WalletService.Coins.CollectionChanged))
				.Merge(Observable.FromEventPattern(Global.WalletService, nameof(Global.WalletService.CoinSpentOrSpenderConfirmed)))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(o => SetBalance())
				.DisposeWith(Disposables);
		}

		private void SetBalance()
		{
			Balance = WalletController.GetBalance().ToString();
		}

        public IObservable<Unit> CoinJoinImpl()
        {
            Deq.Text = "dequeue?";
            return Observable.Start(() =>
            {
                Task.Delay(500);
                Deq.Text = "";
            });
        }
    }
}
