using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using WalletWasabi.Logging;
using NBitcoin;
using WalletWasabi.Blockchain.TransactionProcessing;
using Chaincase.Navigation;
using Splat;
using WalletWasabi.Blockchain.TransactionOutputs;
using System.Collections.Generic;

namespace Chaincase.ViewModels
{
	public class CoinListViewModel : ViewModelBase
	{
        protected Global Global { get; }

        private CompositeDisposable Disposables { get; set; }

        private ReadOnlyObservableCollection<CoinViewModel> _coinViewModels;
        private Money _selectedAmount;
        private bool _selectPrivateSwitchState;
        private bool _isCoinListLoading;
        private bool _isAnyCoinSelected;
        private int _numberSelected;
        private bool _warnCommonOwnership;
        private object SelectionChangedLock { get; } = new object();

        public event EventHandler CoinListShown;
        public event EventHandler<CoinViewModel> SelectionChanged;

        public ReactiveCommand<CoinViewModel, Unit> OpenCoinDetail;

        public CoinListViewModel(bool isPrivate = false)
            : base(Locator.Current.GetService<IViewStackService>())
		{
            Global = Locator.Current.GetService<Global>();
            RootList = new SourceList<CoinViewModel>();
            RootList
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _coinViewModels)
                .Subscribe();

            SelectPrivateSwitchState = true;

            Disposables = Disposables is null ?
               new CompositeDisposable() :
               throw new NotSupportedException($"Cannot open {GetType().Name} before closing it.");

            UpdateRootList();
            Observable
                .Merge(Observable.FromEventPattern<ProcessedResult>(Global.Wallet.TransactionProcessor, nameof(Global.Wallet.TransactionProcessor.WalletRelevantTransactionProcessed)).Select(_ => Unit.Default))
                .Throttle(TimeSpan.FromSeconds(1)) // Throttle TransactionProcessor events adds/removes. 
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    UpdateRootList();
                })
                .DisposeWith(Disposables);

            SelectPrivateSwitchCommand = ReactiveCommand.Create(() =>
            {
                switch (SelectPrivateSwitchState)
                {
                    case true:
                        // FIXME MixUntilAnonymitySet
                        SelectCoins(x => x.AnonymitySet >= Global.Config.PrivacyLevelSome);
                        break;

                    case false:
                        SelectCoins(x => false);
                        SelectPrivateSwitchState = false;
                        break;
                }
            });

            OpenCoinDetail = ReactiveCommand.CreateFromObservable((CoinViewModel cvm) =>
            {
                ViewStackService.PushModal(cvm).Subscribe();
                return Observable.Return(Unit.Default);
            });
        }

        private void UpdateRootList()
        {
            try
            {
                var actual = Global.Wallet.TransactionProcessor.Coins.ToHashSet();
                var old = RootList.Items.ToDictionary(c => c.Model, c => c);

                var coinToRemove = old.Where(c => !actual.Contains(c.Key)).ToArray();
                var coinToAdd = actual.Where(c => !old.ContainsKey(c)).ToArray();

                RootList.RemoveMany(coinToRemove.Select(kp => kp.Value));

                var newCoinViewModels = coinToAdd.Select(c => new CoinViewModel(this, c)).ToArray();
                foreach (var cvm in newCoinViewModels)
                {
                    SubscribeToCoinEvents(cvm);
                }
                RootList.AddRange(newCoinViewModels);

                var allCoins = RootList.Items.ToArray();

                RefreshSelectionCheckBoxes(allCoins);

                foreach (var item in coinToRemove)
                {
                    item.Value.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            finally
            {
                IsCoinListLoading = false;
            }
        }

        private void SubscribeToCoinEvents(CoinViewModel cvm)
        {
            cvm.WhenAnyValue(x => x.IsSelected)
                .Synchronize(SelectionChangedLock) // Use the same lock to ensure thread safety.
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    try
                    {
                        var coins = RootList.Items.ToArray();

                        RefreshSelectionCheckBoxes(coins);
                        var selectedCoins = coins.Where(x => x.IsSelected).ToArray();

                        SelectedAmount = selectedCoins.Sum(x => x.Amount);
                        IsAnyCoinSelected = selectedCoins.Any();

                        WarnCommonOwnership = selectedCoins
                                .Where(c => c.AnonymitySet == 1)
                                .Any(x => selectedCoins.Any(x => x.AnonymitySet > 1));

                        SelectionChanged?.Invoke(this, null);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }
                })
                .DisposeWith(cvm.GetDisposables()); // Subscription will be disposed with the coinViewModel.
        }

        // Vestigial of WasabiWallet for easy update
        private void RefreshSelectionCheckBoxes(CoinViewModel[] coins)
        {
            // FIXME MixUntilAnonymitySet
            SelectPrivateSwitchState = GetCheckBoxesSelectedState(coins, x => x.AnonymitySet >= Global.Config.PrivacyLevelSome);
        }

        private bool GetCheckBoxesSelectedState(CoinViewModel[] allCoins, Func<CoinViewModel, bool> coinFilterPredicate)
        {
            var coins = allCoins.Where(coinFilterPredicate).ToArray();

            bool isAllSelected = coins.All(coin => coin.IsSelected);
            bool isAllDeselected = coins.All(coin => !coin.IsSelected);

            if (isAllDeselected)
            {
                return false;
            }

            if (isAllSelected)
            {
                if (coins.Length != allCoins.Count(coin => coin.IsSelected))
                {
                    return false;
                }
                return true;
            }

            return false;
        }

        private void SelectCoins(Func<CoinViewModel, bool> coinFilterPredicate)
        {
            foreach (var c in CoinList.ToArray())
            {
                c.IsSelected = coinFilterPredicate(c);
            }
        }

        public ReactiveCommand<Unit, Unit> SelectPrivateSwitchCommand { get; }

        public SourceList<CoinViewModel> RootList { get; private set; }

        public IEnumerable<CoinViewModel> CoinList => _coinViewModels;

        public event EventHandler<SmartCoin> DequeueCoinsPressed;

        public bool CanDequeueCoins { get; set; } = false;

        private void ClearRootList() => RootList.Clear();

		public void AfterDismissed()
		{
			ClearRootList();

			Disposables?.Dispose();
		}

        public void PressDequeue(SmartCoin coin)
        {
            DequeueCoinsPressed?.Invoke(this, coin);
        }

        public Money SelectedAmount
        {
            get => _selectedAmount;
            set => this.RaiseAndSetIfChanged(ref _selectedAmount, value);
        }

        public bool SelectPrivateSwitchState
        {
            get => _selectPrivateSwitchState;
            set => this.RaiseAndSetIfChanged(ref _selectPrivateSwitchState, value);
        }

        public bool IsCoinListLoading
        {
            get => _isCoinListLoading;
            set => this.RaiseAndSetIfChanged(ref _isCoinListLoading, value);
        }

        public bool IsAnyCoinSelected
        {
            get => _isAnyCoinSelected;
            set => this.RaiseAndSetIfChanged(ref _isAnyCoinSelected, value);
        }

        public int SelectedCount
        {
            get => _numberSelected;
            set => this.RaiseAndSetIfChanged(ref _numberSelected, value);
        }

        public void SelectOnlyPrivateCoins(bool onlyPrivate)
        {
            foreach (var c in CoinList) {
                c.IsSelected = !onlyPrivate || c.AnonymitySet > 1;
            }
        }

        public bool WarnCommonOwnership
        {
            get => _warnCommonOwnership;
            set => this.RaiseAndSetIfChanged(ref _warnCommonOwnership, value);
        }

    }
}
