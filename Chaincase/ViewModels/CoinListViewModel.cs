using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using WalletWasabi.Logging;
using Xamarin.Forms;
using NBitcoin;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.TransactionProcessing;

namespace Chaincase.ViewModels
{
	public class CoinListViewModel : ViewModelBase
	{
		private CompositeDisposable Disposables { get; set; }

		public SourceList<CoinViewModel> RootList { get; private set; }

        private ReadOnlyObservableCollection<CoinViewModel> _coinViewModels;

        private bool _isCoinListLoading;

        public event EventHandler CoinListShown;

        public ReadOnlyObservableCollection<CoinViewModel> Coins => _coinViewModels;

		public CoinListViewModel(IScreen hostScreen) : base(hostScreen)
		{
            RootList = new SourceList<CoinViewModel>();
            RootList
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _coinViewModels)
                .Subscribe();

			OnOpen();
		}

		public void OnOpen()
		{
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

                        var newCoinViewModels = coinToAdd.Select(c => new CoinViewModel(this, c, HostScreen)).ToArray();
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

		private void ClearRootList() => RootList.Clear();

		public void OnClose()
		{
			ClearRootList();

			Disposables?.Dispose();
		}

        public bool IsCoinListLoading
        {
            get => _isCoinListLoading;
            set => this.RaiseAndSetIfChanged(ref _isCoinListLoading, value);
        }
    }
}
