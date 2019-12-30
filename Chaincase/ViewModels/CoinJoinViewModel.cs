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

            Observable.FromEventPattern(Global.ChaumianClient, nameof(Global.ChaumianClient.CoinQueued))
                .Merge(Observable.FromEventPattern(Global.ChaumianClient, nameof(Global.ChaumianClient.OnDequeue)))
                .Merge(Observable.FromEventPattern(Global.ChaumianClient, nameof(Global.ChaumianClient.StateUpdated)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateStates())
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

        private void UpdateStates()
        {
            var chaumianClient = Global.ChaumianClient;
            if (chaumianClient is null)
            {
                return;
            }
            /*
            AmountQueued = chaumianClient.State.SumAllQueuedCoinAmounts();
            MainWindowViewModel.Instance.CanClose = AmountQueued == Money.Zero;

            var registrableRound = chaumianClient.State.GetRegistrableRoundOrDefault();
            if (registrableRound != default)
            {
                CoordinatorFeePercent = registrableRound.State.CoordinatorFeePercent.ToString();
                UpdateRequiredBtcLabel(registrableRound);
            }
            var mostAdvancedRound = chaumianClient.State.GetMostAdvancedRoundOrDefault();
            if (mostAdvancedRound != default)
            {
                RoundId = mostAdvancedRound.State.RoundId;
                if (!chaumianClient.State.IsInErrorState)
                {
                    Phase = mostAdvancedRound.State.Phase;
                    RoundTimesout = mostAdvancedRound.State.Phase == RoundPhase.InputRegistration ? mostAdvancedRound.State.InputRegistrationTimesout : DateTimeOffset.UtcNow;
                }
                this.RaisePropertyChanged(nameof(Phase));
                this.RaisePropertyChanged(nameof(RoundTimesout));
                PeersRegistered = mostAdvancedRound.State.RegisteredPeerCount;
                PeersNeeded = mostAdvancedRound.State.RequiredPeerCount;
            }
            */
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
