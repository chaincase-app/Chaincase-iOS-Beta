using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NBitcoin;
using ReactiveUI;
using Chaincase.Controllers;
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

namespace Chaincase.ViewModels
{
    public class CoinJoinViewModel : ViewModelBase
    {
        private CompositeDisposable Disposables { get; set; }

        private CoinListViewModel _coinList;
        private string _coordinatorFeePercent;
        private int _requiredPeerCount;

        private Money _requiredBTC;
        private Money _amountQueued;
        private bool _isDequeueBusy;
        private bool _isEnqueueBusy;


        private string _balance;

        public CoinJoinViewModel(CoinListViewModel coinList)
            : base(Locator.Current.GetService<IViewStackService>())
        {
            SetBalance();

            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }

            Disposables = new CompositeDisposable();
            CoinList = coinList;

            Observable
                .FromEventPattern<SmartCoin>(CoinList, nameof(CoinList.DequeueCoinsPressed))
                .Subscribe(async x => await DoDequeueAsync(x.EventArgs));

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
                RequiredPeerCount = mostAdvancedRound.State.RequiredPeerCount;
            }
            else
            {
                RequiredPeerCount = 100;
            }

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
            Balance = WalletController.GetBalance(Global.Network).ToString();
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
                RequiredPeerCount = mostAdvancedRound.State.RequiredPeerCount;
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

        public int RequiredPeerCount
        {
            get => _requiredPeerCount;
            set => this.RaiseAndSetIfChanged(ref _requiredPeerCount, value);
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
