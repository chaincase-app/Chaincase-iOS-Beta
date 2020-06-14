using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NBitcoin;
using ReactiveUI;
using System.Linq;
using WalletWasabi.CoinJoin.Client.Rounds;
using System.Collections.Generic;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Helpers;
using System.Security;
using System.Text;
using WalletWasabi.Logging;
using Chaincase.Navigation;
using Splat;
using WalletWasabi.CoinJoin.Client.Clients.Queuing;
using WalletWasabi.CoinJoin.Common.Models;
using Chaincase.Models;
using Xamarin.Forms;

namespace Chaincase.ViewModels
{
    public class CoinJoinViewModel : ViewModelBase
    {
        protected Global Global { get; }

        private CompositeDisposable Disposables { get; set; }

        private INotificationManager notificationManager;
        private int notificationNumber = 0;

        private CoinListViewModel _coinList;
        private string _coordinatorFeePercent;
        private int _peersRegistered;
        private int _peersNeeded;

        private RoundPhaseState _roundPhaseState;
        private DateTimeOffset _roundTimesout;
        private TimeSpan _timeLeftTillRoundTimeout;
        private Money _requiredBTC;
        private Money _amountQueued;
        private bool _isDequeueBusy;
        private bool _isEnqueueBusy;


        private string _balance;

        public CoinJoinViewModel(CoinListViewModel coinList)
            : base(Locator.Current.GetService<IViewStackService>())
        {
            Global = Locator.Current.GetService<Global>();
            SetBalance();
            notificationManager = DependencyService.Get<INotificationManager>();
            // TODO tell them why they need notifications to CoinJOin
            notificationManager.Initialize();
            notificationManager.NotificationReceived += (sender, eventArgs) =>
            {
                var evtData = (NotificationEventArgs)eventArgs;
                ShowNotification(evtData.Title, evtData.Message);
            };

            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }

            Disposables = new CompositeDisposable();
            CoinList = coinList;

            Observable
                .FromEventPattern<SmartCoin>(CoinList, nameof(CoinList.DequeueCoinsPressed))
                .Subscribe(async x => await DoDequeueAsync(x.EventArgs));

            this.WhenAnyValue(x => x.RoundTimesout)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    TimeSpan left = RoundTimesout - DateTimeOffset.UtcNow;
                    TimeLeftTillRoundTimeout = left > TimeSpan.Zero ? left : TimeSpan.Zero; // Make sure cannot be less than zero.
                });

            AmountQueued = Money.Zero;

            var registrableRound = Global.Wallet.ChaumianClient.State.GetRegistrableRoundOrDefault();

            CoordinatorFeePercent = registrableRound?.State?.CoordinatorFeePercent.ToString() ?? "0.003";

            Observable.FromEventPattern(Global.Wallet.ChaumianClient, nameof(Global.Wallet.ChaumianClient.CoinQueued))
                .Merge(Observable.FromEventPattern(Global.Wallet.ChaumianClient, nameof(Global.Wallet.ChaumianClient.OnDequeue)))
                .Merge(Observable.FromEventPattern(Global.Wallet.ChaumianClient, nameof(Global.Wallet.ChaumianClient.StateUpdated)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateStates())
                .DisposeWith(Disposables);

            ClientRound mostAdvancedRound = Global.Wallet.ChaumianClient?.State?.GetMostAdvancedRoundOrDefault();

            if (mostAdvancedRound != default)
            {
                RoundPhaseState = new RoundPhaseState(mostAdvancedRound.State.Phase, Global.Wallet.ChaumianClient?.State.IsInErrorState ?? false);
                RoundTimesout = mostAdvancedRound.State.Phase == RoundPhase.InputRegistration ? mostAdvancedRound.State.InputRegistrationTimesout : DateTimeOffset.UtcNow;
                PeersRegistered = mostAdvancedRound.State.RegisteredPeerCount;
                PeersNeeded = mostAdvancedRound.State.RequiredPeerCount;
            }
            else
            {
                RoundPhaseState = new RoundPhaseState(RoundPhase.InputRegistration, false);
                RoundTimesout = DateTimeOffset.UtcNow;
                PeersRegistered = 0;
                PeersNeeded = 100;
            }

            Observable.Interval(TimeSpan.FromSeconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    TimeSpan left = RoundTimesout - DateTimeOffset.UtcNow;
                    TimeLeftTillRoundTimeout = left > TimeSpan.Zero ? left : TimeSpan.Zero; // Make sure cannot be less than zero.
                }).DisposeWith(Disposables);

            CoinJoinCommand = ReactiveCommand.CreateFromTask<string, bool>(DoEnqueueAsync);

            var canPromptPassword = this.WhenAnyValue(
                x => x.CoinList.SelectedAmount,
                x => x.RequiredBTC,
                (amnt, rBTC) =>
                {
                    return !(rBTC is null) && !(amnt is null) && amnt >= rBTC;
                });
            _promptViewModel = new PasswordPromptViewModel("CoinJoin");
            _promptViewModel.ValidatePasswordCommand.Subscribe(async validPassword =>
            {
                if (validPassword != null)
                {
                    await DoEnqueueAsync(validPassword);
                    await ViewStackService.PopModal();
                }
            });

            PromptCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                ViewStackService.PushModal(_promptViewModel).Subscribe();
                return Observable.Return(Unit.Default);
            }, canPromptPassword);
        }

        private void SetBalance()
        {
            // ToProperty
            Balance = (
                Enumerable.Where(Global.Wallet.Coins,
                    c => c.Unspent && !c.SpentAccordingToBackend
                ).Sum(c => (long?)c.Amount) ?? 0
                ).ToString();
        }

        private async Task DoDequeueAsync(params SmartCoin[] coins)
            => await DoDequeueAsync(coins as IEnumerable<SmartCoin>);

        private async Task DoDequeueAsync(IEnumerable<SmartCoin> coins)
        {
            IsDequeueBusy = true;
            try
            {
                if (!coins.Any())
                {
                    return;
                }

                try
                {
                    await Global.Wallet.ChaumianClient.DequeueCoinsFromMixAsync(coins.ToArray(), DequeueReason.UserRequested);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex);
                }
            }
            finally
            {
                IsDequeueBusy = false;
            }
        }

        private async Task<bool> DoEnqueueAsync(string password)
        {
            IsEnqueueBusy = true;
            var coins = CoinList.Coins.Where(c => c.IsSelected).Select(c => c.Model);
            try
            {
                if (!coins.Any())
                {
                    // should never get to this page if there aren't sufficient coins
                    return false;
                }
                try
                {
                    PasswordHelper.GetMasterExtKey(Global.Wallet.KeyManager, password, out string compatiblityPassword); // If the password is not correct we throw.

                    if (compatiblityPassword != null)
                    {
                        password = compatiblityPassword;
                    }

                    await Global.Wallet.ChaumianClient.QueueCoinsToMixAsync(password, coins.ToArray());
                    ScheduleConfirmNotification(null, null);
                    return true;
                }
                catch (SecurityException ex)
                {
                    // pobably shaking in the view
                    // NotificationHelpers.Error(ex.Message, "");
                }
                catch (Exception ex)
                {
                    var builder = new StringBuilder(ex.ToTypeMessageString());
                    if (ex is AggregateException aggex)
                    {
                        foreach (var iex in aggex.InnerExceptions)
                        {
                            builder.Append(Environment.NewLine + iex.ToTypeMessageString());
                        }
                    }
                    // NotificationHelpers.Error(builder.ToString());
                    Logger.LogError(ex);
                }
            }
            finally
            {
                IsEnqueueBusy = false;
            }
            return false;
        }

        void ScheduleConfirmNotification(object sender, EventArgs e)
        {
            const int NOTIFY_TIMEOUT_DELTA = 90; // seconds

            var timeoutSeconds = TimeLeftTillRoundTimeout.TotalSeconds;
            if (timeoutSeconds < NOTIFY_TIMEOUT_DELTA)
                // Just encourage users to keep the app open
                // & prepare CoinJoin to background if possible.
                return;

            // Takes about 30 seconds to start Tor & connect
            var confirmTime = DateTime.Now.AddSeconds(timeoutSeconds);
            string title = $"Go Private";
            string message = string.Format("Open Chaincase before {0:t}\n to complete the CoinJoin.", confirmTime);

            var timeToNotify = timeoutSeconds - NOTIFY_TIMEOUT_DELTA;
            notificationManager.ScheduleNotification(title, message, timeToNotify);
        }

        void ShowNotification(string title, string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                // log a notification fired:w
                Logger.LogInfo($"NOTIFICATION: #{title} #{message}");
            });
        }

        private void UpdateStates()
        {
            var chaumianClient = Global.Wallet.ChaumianClient;
            if (chaumianClient is null)
            {
                return;
            }

            AmountQueued = chaumianClient.State.SumAllQueuedCoinAmounts();

            var registrableRound = chaumianClient.State.GetRegistrableRoundOrDefault();
            if (registrableRound != default)
            {
                CoordinatorFeePercent = registrableRound.State.CoordinatorFeePercent.ToString();
                UpdateRequiredBtcLabel(registrableRound);
            }

            var mostAdvancedRound = chaumianClient.State.GetMostAdvancedRoundOrDefault();
            if (mostAdvancedRound != default)
            {
                if (!chaumianClient.State.IsInErrorState)
                {
                    RoundPhaseState = new RoundPhaseState(mostAdvancedRound.State.Phase, false);
                    RoundTimesout = mostAdvancedRound.State.Phase == RoundPhase.InputRegistration ? mostAdvancedRound.State.InputRegistrationTimesout : DateTimeOffset.UtcNow;
                }
                else
                {
                    RoundPhaseState = new RoundPhaseState(RoundPhaseState.Phase, true);
                }
                this.RaisePropertyChanged(nameof(RoundPhaseState));
                this.RaisePropertyChanged(nameof(RoundTimesout));
                PeersRegistered = mostAdvancedRound.State.RegisteredPeerCount;
                PeersNeeded = mostAdvancedRound.State.RequiredPeerCount;
            }
        }

        private void UpdateRequiredBtcLabel(ClientRound registrableRound)
        {
            if (Global.WalletManager is null)
            {
                return; // Otherwise NullReferenceException at shutdown.
            }

            if (registrableRound == default)
            {
                if (RequiredBTC == default)
                {
                    RequiredBTC = Money.Zero;
                }
            }
            else
            {
                var coins = Global.Wallet.Coins;
                var queued = coins.CoinJoinInProcess();
                if (queued.Any())
                {
                    RequiredBTC = registrableRound.State.CalculateRequiredAmount(Global.Wallet.ChaumianClient.State.GetAllQueuedCoinAmounts().ToArray());
                }
                else
                {
                    var available = coins.Confirmed().Available();
                    RequiredBTC = available.Any()
                        ? registrableRound.State.CalculateRequiredAmount(available.Where(x => x.AnonymitySet < Global.Config.PrivacyLevelStrong).Select(x => x.Amount).ToArray())
                        : registrableRound.State.CalculateRequiredAmount();
                }
            }
        }

        public CoinListViewModel CoinList
        {
            get => _coinList;
            set => this.RaiseAndSetIfChanged(ref _coinList, value);
        }

        public Money AmountQueued
        {
            get => _amountQueued;
            set => this.RaiseAndSetIfChanged(ref _amountQueued, value);
        }

        public Money RequiredBTC
        {
            get => _requiredBTC;
            set => this.RaiseAndSetIfChanged(ref _requiredBTC, value);
        }

        public string CoordinatorFeePercent
        {
            get => _coordinatorFeePercent;
            set => this.RaiseAndSetIfChanged(ref _coordinatorFeePercent, value);
        }

        public string Balance
        {
            get => _balance;
            set => this.RaiseAndSetIfChanged(ref _balance, value);
        }

        public int PeersNeeded
        {
            get => _peersNeeded;
            set => this.RaiseAndSetIfChanged(ref _peersNeeded, value);
        }

        public int PeersRegistered
        {
            get => _peersRegistered;
            set => this.RaiseAndSetIfChanged(ref _peersRegistered, value);
        }

        public RoundPhaseState RoundPhaseState
        {
            get => _roundPhaseState;
            set => this.RaiseAndSetIfChanged(ref _roundPhaseState, value);
        }

        public DateTimeOffset RoundTimesout
        {
            get => _roundTimesout;
            set => this.RaiseAndSetIfChanged(ref _roundTimesout, value);
        }

        public TimeSpan TimeLeftTillRoundTimeout
        {
            get => _timeLeftTillRoundTimeout;
            set => this.RaiseAndSetIfChanged(ref _timeLeftTillRoundTimeout, value);
        }

        public bool IsEnqueueBusy
        {
            get => _isEnqueueBusy;
            set => this.RaiseAndSetIfChanged(ref _isEnqueueBusy, value);
        }

        public bool IsDequeueBusy
        {
            get => _isDequeueBusy;
            set => this.RaiseAndSetIfChanged(ref _isDequeueBusy, value);
        }

        public ReactiveCommand<string, bool> CoinJoinCommand { get; }
        private PasswordPromptViewModel _promptViewModel;
        public ReactiveCommand<Unit, Unit> PromptCommand { get; }
    }
}
