using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NBitcoin;
using ReactiveUI;
using Chaincase.Controllers;
using System.Diagnostics;
using System.Linq;
using WalletWasabi.CoinJoin.Client.Rounds;
using System.Collections.Generic;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Services;
using System.Security;
using System.Text;
using WalletWasabi.Logging;

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


        private string _balance;
        private string accept;

        public CoinJoinViewModel(IScreen hostScreen, CoinListViewModel coinList) : base(hostScreen)
        {
            SetBalance();

            if (Disposables != null)
            {
                throw new Exception("Wallet opened before it was closed.");
            }

            Disposables = new CompositeDisposable();
            CoinList = coinList;

            AmountQueued = Money.Zero;

            var registrableRound = Global.ChaumianClient.State.GetRegistrableRoundOrDefault();
            
            CoordinatorFeePercent = registrableRound?.State?.CoordinatorFeePercent.ToString() ?? "0.003";

            CoinJoinCommand = ReactiveCommand.CreateFromTask<string>(async (password) => await  DoEnqueueAsync(CoinList.Coins.Select(c => c.Model), password)); 

            Observable.FromEventPattern(Global.ChaumianClient, nameof(Global.ChaumianClient.CoinQueued))
                .Merge(Observable.FromEventPattern(Global.ChaumianClient, nameof(Global.ChaumianClient.OnDequeue)))
                .Merge(Observable.FromEventPattern(Global.ChaumianClient, nameof(Global.ChaumianClient.StateUpdated)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateStates())
                .DisposeWith(Disposables);

            ClientRound mostAdvancedRound = Global.ChaumianClient?.State?.GetMostAdvancedRoundOrDefault();

            if (mostAdvancedRound != default)
            {
                RequiredPeerCount = mostAdvancedRound.State.RequiredPeerCount;
            }
            else
            {
                RequiredPeerCount = 100;
            }

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

        private async Task DoEnqueueAsync(IEnumerable<SmartCoin> coins, string password)
        {
            IsEnqueueBusy = true;
            try
            {
                if (!coins.Any())
                {
                    // should never get to this page if there aren't sufficient coins
                    // NotificationHelpers.Warning("No coins are selected.", "");
                    return;
                }
                try
                {
                    PasswordHelper.GetMasterExtKey(Global.WalletService.KeyManager, password, out string compatiblityPassword); // If the password is not correct we throw.

                    if (compatiblityPassword != null)
                    {
                        password = compatiblityPassword;
                        // NotificationHelpers.Warning(PasswordHelper.CompatibilityPasswordWarnMessage);
                    }

                    await Global.ChaumianClient.QueueCoinsToMixAsync(password, coins.ToArray());
                }
                catch (SecurityException ex)
                {
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
        }

        private void UpdateStates()
        {
            var chaumianClient = Global.ChaumianClient;
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
            if (Global.WalletService is null)
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
                var coins = Global.WalletService.Coins;
                var queued = coins.CoinJoinInProcess();
                if (queued.Any())
                {
                    RequiredBTC = registrableRound.State.CalculateRequiredAmount(Global.ChaumianClient.State.GetAllQueuedCoinAmounts().ToArray());
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
        public bool IsEnqueueBusy { get; private set; }

        public ReactiveCommand<string, Unit> CoinJoinCommand;

        public async Task CoinJoin(string password)
        {
            Debug.WriteLine(password);
            if (password.Equals("bosco"))
            {
                await Task.Delay(4444);
            } else
            {
                await Task.Delay(200);
            }
        }
    }
}
