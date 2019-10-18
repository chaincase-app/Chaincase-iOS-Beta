using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using WalletWasabi.Logging;
using WalletWasabi.Models;
using Chaincase.Models;
using Chaincase.Navigation;
using Xamarin.Forms;
using NBitcoin;

namespace Chaincase.ViewModels
{
	public class CoinListViewModel : ViewModelBase
	{

		private Money _selectedAmount;

		public Money SelectedAmount
		{
			get => _selectedAmount;
			set => this.RaiseAndSetIfChanged(ref _selectedAmount, value);
		}

		private string _selectedAmountText;
		public String SelectedAmountText
		{
			get => _selectedAmountText;
			set => this.RaiseAndSetIfChanged(ref _selectedAmountText, $"{value} BTC Selected");
		}

		public event EventHandler<CoinViewModel> SelectionChanged;

		private CompositeDisposable Disposables { get; set; }

		public SourceList<CoinViewModel> RootList { get; private set; }

		private ReadOnlyObservableCollection<CoinViewModel> _coinViewModels;

		public ReadOnlyObservableCollection<CoinViewModel> Coins => _coinViewModels;

		public ReactiveCommand<Unit, Unit> BackCommand { get; }

		public CoinListViewModel(IScreen hostScreen) : base(hostScreen)
		{
			SelectedAmountText = "0";
			RootList = new SourceList<CoinViewModel>();
			RootList.Connect()
				.OnItemRemoved(x => x.UnsubscribeEvents())
				.Bind(out _coinViewModels)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe();


			BackCommand = hostScreen.Router.NavigateBack;
			OnOpen();

		}

		public void OnOpen()
		{

			Disposables = new CompositeDisposable();

			foreach (var sc in Global.WalletService.Coins.Where(sc => sc.Unspent))
			{
				var newCoinVm = new CoinViewModel(this, sc, _hostScreen);
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
									var newCoinVm = new CoinViewModel(this, c, _hostScreen);
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
		}

		private void ClearRootList() => RootList.Clear();

		public void OnClose()
		{
			ClearRootList();

			Disposables?.Dispose();
		}

		public void OnCoinIsSelectedChanged(CoinViewModel cvm)
		{
			SelectionChanged?.Invoke(this, cvm);
			SelectedAmount = Coins.Where(x => x.IsSelected).Sum(x => x.Amount);
			SelectedAmountText = SelectedAmount.ToString();
		}

	}
}
