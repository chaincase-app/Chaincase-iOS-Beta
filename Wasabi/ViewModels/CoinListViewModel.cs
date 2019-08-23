using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using WalletWasabi.Logging;
using WalletWasabi.Models;
using Wasabi.Models;
using Wasabi.Navigation;
using Xamarin.Forms;

namespace Wasabi.ViewModels
{
	public class CoinListViewModel : ViewModelBase
	{
		private CompositeDisposable Disposables { get; set; }

		public SourceList<CoinViewModel> RootList { get; private set; }

		private ReadOnlyObservableCollection<CoinViewModel> _coinViewModels;
		private SortExpressionComparer<CoinViewModel> _myComparer;

		private CoinViewModel _selectedCoin;
		private bool? _selectAllCheckBoxState;
		private bool? _selectPrivateCheckBoxState;
		private bool? _selectNonPrivateCheckBoxState;
		private GridLength _coinJoinStatusWidth;

		public ReactiveCommand<Unit, Unit> EnqueueCoin { get; }
		public ReactiveCommand<Unit, Unit> DequeueCoin { get; }
		public ReactiveCommand<Unit, Unit> SelectAllCheckBoxCommand { get; }
		public ReactiveCommand<Unit, Unit> SelectPrivateCheckBoxCommand { get; }
		public ReactiveCommand<Unit, Unit> SelectNonPrivateCheckBoxCommand { get; }
		public ReactiveCommand<Unit, Unit> SortCommand { get; }
		public ICommand BackCommand { get; }

		public event EventHandler DequeueCoinsPressed;

		public event EventHandler<CoinViewModel> SelectionChanged;

		public ReadOnlyObservableCollection<CoinViewModel> Coins => _coinViewModels;

		private SortExpressionComparer<CoinViewModel> MyComparer
		{
			get => _myComparer;
			set => this.RaiseAndSetIfChanged(ref _myComparer, value);
		}

		public CoinViewModel SelectedCoin
		{
			get => _selectedCoin;
			set
			{
				this.RaiseAndSetIfChanged(ref _selectedCoin, value);
				this.RaisePropertyChanged(nameof(CanDeqeue));
			}
		}

		public bool CanDeqeue => SelectedCoin is null ? false : SelectedCoin.CoinJoinInProgress;

		public bool? SelectAllCheckBoxState
		{
			get => _selectAllCheckBoxState;
			set => this.RaiseAndSetIfChanged(ref _selectAllCheckBoxState, value);
		}

		public bool? SelectPrivateCheckBoxState
		{
			get => _selectPrivateCheckBoxState;
			set => this.RaiseAndSetIfChanged(ref _selectPrivateCheckBoxState, value);
		}

		public bool? SelectNonPrivateCheckBoxState
		{
			get => _selectNonPrivateCheckBoxState;
			set => this.RaiseAndSetIfChanged(ref _selectNonPrivateCheckBoxState, value);
		}

		public GridLength CoinJoinStatusWidth
		{
			get => _coinJoinStatusWidth;
			set => this.RaiseAndSetIfChanged(ref _coinJoinStatusWidth, value);
		}
		public IObservable<IComparer<CoinViewModel>> Ascending { get; }

		private bool? GetCheckBoxesSelectedState(Func<CoinViewModel, bool> coinFilterPredicate)
		{
			var coins = Coins.Where(coinFilterPredicate).ToArray();
			bool IsAllSelected = true;
			foreach (CoinViewModel coin in coins)
			{
				if (!coin.IsSelected)
				{
					IsAllSelected = false;
					break;
				}
			}

			bool IsAllDeselected = true;
			foreach (CoinViewModel coin in coins)
			{
				if (coin.IsSelected)
				{
					IsAllDeselected = false;
					break;
				}
			}

			if (IsAllDeselected)
			{
				return false;
			}

			if (IsAllSelected)
			{
				return true;
			}

			return null;
		}

		private void SelectAllCoins(bool valueOfSelected, Func<CoinViewModel, bool> coinFilterPredicate)
		{
			var coins = Coins.Where(coinFilterPredicate).ToArray();
			foreach (var c in coins)
			{
				c.IsSelected = valueOfSelected;
			}
		}

		public CoinListViewModel(INavigationService navigationService) : base(navigationService)
		{
			RootList = new SourceList<CoinViewModel>();
			RootList.Connect()
				.OnItemRemoved(x => x.UnsubscribeEvents())
				.Bind(out _coinViewModels)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe();

			EnqueueCoin = ReactiveCommand.Create(() =>
			{
				if (SelectedCoin is null)
				{
					return;
				}
				//await Global.ChaumianClient.QueueCoinsToMixAsync()
			});

			DequeueCoin = ReactiveCommand.Create(() =>
			{
				if (SelectedCoin is null)
				{
					return;
				}

				DequeueCoinsPressed?.Invoke(this, EventArgs.Empty);
			}, this.WhenAnyValue(x => x.CanDeqeue)
				.ObserveOn(RxApp.MainThreadScheduler));

			SelectAllCheckBoxCommand = ReactiveCommand.Create(() =>
			{
				//Global.WalletService.Coins.First(c => c.Unspent).Unspent = false;
				switch (SelectAllCheckBoxState)
				{
					case true:
						SelectAllCoins(true, x => true);
						break;

					case false:
						SelectAllCoins(false, x => true);
						break;

					case null:
						SelectAllCoins(false, x => true);
						SelectAllCheckBoxState = false;
						break;
				}
			});


			SelectPrivateCheckBoxCommand = ReactiveCommand.Create(() =>
			{
				switch (SelectPrivateCheckBoxState)
				{
					case true:
						SelectAllCoins(true, x => x.AnonymitySet >= Global.Config.PrivacyLevelStrong);
						break;

					case false:
						SelectAllCoins(false, x => x.AnonymitySet >= Global.Config.PrivacyLevelStrong);
						break;

					case null:
						SelectAllCoins(false, x => x.AnonymitySet >= Global.Config.PrivacyLevelStrong);
						SelectPrivateCheckBoxState = false;
						break;
				}
			});

			SelectNonPrivateCheckBoxCommand = ReactiveCommand.Create(() =>
			{
				switch (SelectNonPrivateCheckBoxState)
				{
					case true:
						SelectAllCoins(true, x => x.AnonymitySet < Global.Config.PrivacyLevelStrong);
						break;

					case false:
						SelectAllCoins(false, x => x.AnonymitySet < Global.Config.PrivacyLevelStrong);
						break;

					case null:
						SelectAllCoins(false, x => x.AnonymitySet < Global.Config.PrivacyLevelStrong);
						SelectNonPrivateCheckBoxState = false;
						break;
				}
			});

			BackCommand = new Command(() => navigationService.NavigateBack());
			OnOpen();
		}

		public void OnOpen()
		{
			Disposables = new CompositeDisposable();

			foreach (var sc in Global.WalletService.Coins.Where(sc => sc.Unspent))
			{
				var newCoinVm = new CoinViewModel(this, sc, _navigationService);
				newCoinVm.SubscribeEvents();
				RootList.Add(newCoinVm);
			}

			Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(Global.WalletService.Coins, nameof(Global.WalletService.Coins.CollectionChanged))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(x =>
				{
					var e = x.EventArgs;
					try
					{
						switch (e.Action)
						{
							case NotifyCollectionChangedAction.Add:
								foreach (SmartCoin c in e.NewItems.Cast<SmartCoin>().Where(sc => sc.Unspent))
								{
									var newCoinVm = new CoinViewModel(this, c, _navigationService);
									newCoinVm.SubscribeEvents();
									RootList.Add(newCoinVm);
								}
								break;

							case NotifyCollectionChangedAction.Remove:
								foreach (var c in e.OldItems.Cast<SmartCoin>())
								{
									CoinViewModel toRemove = RootList.Items.FirstOrDefault(cvm => cvm.Model == c);
									if (toRemove != default)
									{
										RootList.Remove(toRemove);
									}
								}
								break;

							case NotifyCollectionChangedAction.Reset:
								ClearRootList();
								break;
						}
					}
					catch (Exception ex)
					{
						Logger.LogDebug<CoinListViewModel>(ex);
					}
				}).DisposeWith(Disposables);

			SetSelections();
			SetCoinJoinStatusWidth();
		}

		private void ClearRootList() => RootList.Clear();

		public void OnClose()
		{
			ClearRootList();

			Disposables?.Dispose();
		}

		private void SetSelections()
		{
			SelectAllCheckBoxState = GetCheckBoxesSelectedState(x => true);
			SelectPrivateCheckBoxState = GetCheckBoxesSelectedState(x => x.AnonymitySet >= Global.Config.PrivacyLevelStrong);
			SelectNonPrivateCheckBoxState = GetCheckBoxesSelectedState(x => x.AnonymitySet < Global.Config.PrivacyLevelStrong);
		}

		private void SetCoinJoinStatusWidth()
		{
			if (Coins.Any(x => x.Status == SmartCoinStatus.MixingConnectionConfirmation
				 || x.Status == SmartCoinStatus.MixingInputRegistration
				 || x.Status == SmartCoinStatus.MixingOnWaitingList
				 || x.Status == SmartCoinStatus.MixingOutputRegistration
				 || x.Status == SmartCoinStatus.MixingSigning
				 || x.Status == SmartCoinStatus.MixingWaitingForConfirmation
				 || x.Status == SmartCoinStatus.SpentAccordingToBackend))
			{
				CoinJoinStatusWidth = new GridLength(180);
			}
			else
			{
				CoinJoinStatusWidth = new GridLength(0);
			}
		}

		public void OnCoinIsSelectedChanged(CoinViewModel cvm)
		{
			SetSelections();
			SelectionChanged?.Invoke(this, cvm);
		}

		public void OnCoinStatusChanged()
		{
			SetCoinJoinStatusWidth();
		}

		public void OnCoinUnspentChanged(CoinViewModel cvm)
		{
			if (!cvm.Unspent)
			{
				RootList.Remove(cvm);
			}

			SetSelections();
			SetCoinJoinStatusWidth();
		}
	}
}
