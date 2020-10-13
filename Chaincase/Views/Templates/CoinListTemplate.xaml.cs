using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System.Linq;
using System;

namespace Chaincase.Views.Templates
{
	public partial class CoinListTemplate : ReactiveContentView<CoinListViewModel>
	{
		public CoinListTemplate()
		{
			InitializeComponent();

			this.WhenActivated(d =>
			{
				this.OneWayBind(ViewModel,
					vm => vm.CoinList,
					v => v.Coins.ItemsSource)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.CoinList,
					v => v.Coins.IsVisible,
					coins => coins.Count() > 0)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.CoinList,
					v => v.EmptyWalletLabel.IsVisible,
					coins => coins.Count() == 0)
					.DisposeWith(d);
			});

			Coins.ItemTapped += (_, e) =>
			{
				CoinViewModel cvm = (CoinViewModel)e.Item;
				ViewModel.OpenCoinDetail.Execute(cvm).Subscribe();
			}; ;
		}
	}
}
