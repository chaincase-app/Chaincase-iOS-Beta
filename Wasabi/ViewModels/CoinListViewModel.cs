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

		public ReactiveCommand<Unit, Unit> BackCommand { get; }

		public event EventHandler<CoinViewModel> SelectionChanged;

		public ReadOnlyObservableCollection<CoinViewModel> Coins => _coinViewModels;

		public CoinListViewModel(IScreen hostScreen) : base(hostScreen)
		{
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

	}
}
