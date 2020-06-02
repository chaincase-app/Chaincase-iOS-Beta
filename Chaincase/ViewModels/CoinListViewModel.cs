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
        private string _selectedAmountText;
        private Money _selectedAmount;
        private bool? _selectPrivateSwitchState;
        private bool _isCoinListLoading;
        private bool _isAnyCoinSelected;
        private bool _warnCommonOwnership;
        private object SelectionChangedLock { get; } = new object();

        public event EventHandler CoinListShown;
        public event EventHandler<CoinViewModel> SelectionChanged;

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

            SelectPrivateSwitchState = false;

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

                    case null:
                    case false:
                        SelectCoins(x => false);
                        SelectPrivateSwitchState = false;
                        break;
                }
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

        private void SelectCoins(Func<CoinViewModel, bool> coinFilterPredicate)
        {
            foreach (var c in Coins.ToArray())
            {
                c.IsSelected = coinFilterPredicate(c);
            }
        }

        public ReactiveCommand<Unit, Unit> SelectPrivateSwitchCommand { get; }

        public SourceList<CoinViewModel> RootList { get; private set; }

        public IEnumerable<CoinViewModel> Coins => _coinViewModels.Where(c => !SelectOnlyFromPrivate || c.AnonymitySet > 1);

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

        public string SelectedAmountText
        {
            get => _selectedAmountText;
            set => this.RaiseAndSetIfChanged(ref _selectedAmountText, $"{value} BTC Selected");
        }

        public Money SelectedAmount
        {
            get => _selectedAmount;
            set => this.RaiseAndSetIfChanged(ref _selectedAmount, value);
        }

        public bool? SelectPrivateSwitchState
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

        public bool SelectOnlyFromPrivate = false;

        public void SelectOnlyPrivateCoins(bool onlyPrivate)
        {
            SelectOnlyFromPrivate = onlyPrivate;
            foreach (var c in Coins) {
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
