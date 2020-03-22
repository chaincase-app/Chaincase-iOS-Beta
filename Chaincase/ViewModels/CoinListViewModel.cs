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

namespace Chaincase.ViewModels
{
	public class CoinListViewModel : ViewModelBase
	{
		private CompositeDisposable Disposables { get; set; }

        private ReadOnlyObservableCollection<CoinViewModel> _coinViewModels;

        private string _selectedAmountText;
        private Money _selectedAmount;
        private bool _isCoinListLoading;
        private bool _isAnyCoinSelected;
        private bool _warnCommonOwnership;
        private object SelectionChangedLock { get; } = new object();

        public event EventHandler CoinListShown;
        public event EventHandler<CoinViewModel> SelectionChanged;

        public CoinListViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
            RootList = new SourceList<CoinViewModel>();
            RootList
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _coinViewModels)
                .Subscribe();

            Disposables = Disposables is null ?
               new CompositeDisposable() :
               throw new NotSupportedException($"Cannot open {GetType().Name} before closing it.");

            Observable
                .Merge(Observable.FromEventPattern<ProcessedResult>(Global.WalletService.TransactionProcessor, nameof(Global.WalletService.TransactionProcessor.WalletRelevantTransactionProcessed)).Select(_ => Unit.Default))
                .Throttle(TimeSpan.FromSeconds(1)) // Throttle TransactionProcessor events adds/removes.
                .Merge(Observable.FromEventPattern(this, nameof(CoinListShown), RxApp.MainThreadScheduler).Select(_ => Unit.Default)) // Load the list immediately.
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args =>
                {
                    try
                    {
                        var actual = Global.WalletService.TransactionProcessor.Coins.ToHashSet();
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
                })
                .DisposeWith(Disposables);

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

        public SourceList<CoinViewModel> RootList { get; private set; }

        public ReadOnlyObservableCollection<CoinViewModel> Coins => _coinViewModels;

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

        public bool WarnCommonOwnership
        {
            get => _warnCommonOwnership;
            set => this.RaiseAndSetIfChanged(ref _warnCommonOwnership, value);
        }

    }
}
