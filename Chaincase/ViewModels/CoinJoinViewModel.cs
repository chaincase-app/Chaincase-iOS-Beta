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
	public class CoinJoinViewModel : ViewModelBase
	{
		private CompositeDisposable Disposables { get; set; }

        private CoinListViewModel _coinList;
        private string _coordinatorFeePercent;
        private int _peersNeeded;
        private Money _requiredBTC;
        
        private String _balance;

		public ReactiveCommand<Unit, Unit> NavigateBack { get; private set; }
        public ReactiveCommand<Unit, Unit> CoinJoin { get; private set; }
        readonly ObservableAsPropertyHelper<bool> _isJoining;
        public bool IsJoining { get { return _isJoining.Value; } }

        public CoinJoinViewModel(IScreen hostScreen) : base(hostScreen)
        {
            SetBalance();

            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }

            Disposables = new CompositeDisposable();
            CoinList = new CoinListViewModel(hostScreen);

            NavigateBack = HostScreen.Router.NavigateBack;

            CoinJoin = ReactiveCommand.CreateFromObservable(CoinJoinImpl);
            CoinJoin.IsExecuting.ToProperty(this, x => x.IsJoining, out _isJoining);
            CoinJoin.ThrownExceptions.Subscribe(ex => Logger.LogError(ex));

            Observable.FromEventPattern(Global.WalletService.Coins, nameof(Global.WalletService.Coins.CollectionChanged))
				.Merge(Observable.FromEventPattern(Global.WalletService, nameof(Global.WalletService.CoinSpentOrSpenderConfirmed)))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(o => SetBalance())
				.DisposeWith(Disposables);

            OnOpen();
		}

        private void OnOpen()
        {
            var registrableRound = Global.ChaumianClient.State.GetRegistrableRoundOrDefault();


        }


		private void SetBalance()
		{
			Balance = WalletController.GetBalance().ToString();
		}

        public IObservable<Unit> CoinJoinImpl()
        {
            return Observable.Start(() =>
            {
                Task.Delay(500);
            });
        }

        public CoinListViewModel CoinList
        {
            get => _coinList;
            set => this.RaiseAndSetIfChanged(ref _coinList, value);
        }

        public String Balance
        {
            get => _balance;
            set => this.RaiseAndSetIfChanged(ref _balance, value);
        }
    }
}
