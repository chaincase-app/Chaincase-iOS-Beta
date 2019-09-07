using System;
using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Wasabi.ViewModels;

namespace Wasabi.Views
{
	public partial class CoinListPage : ReactiveContentPage<CoinListViewModel>
	{
		public CoinListPage()
		{

			InitializeComponent();
			this.WhenActivated(disposables =>
			{
				this.OneWayBind(ViewModel, vm => vm.Coins, v => v.Coins.ItemsSource).DisposeWith(disposables);
			});
		}
	}
}
