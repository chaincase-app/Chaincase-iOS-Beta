using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Wasabi.ViewModels;

namespace Wasabi.Views.Templates
{
	public partial class CoinListTemplate : ReactiveContentView<CoinListViewModel>
	{
		public CoinListTemplate()
		{
			InitializeComponent();

			this.WhenActivated(disposable =>
			{
				this.OneWayBind(ViewModel, vm => vm.Coins, v => v.Coins.ItemsSource)
				.DisposeWith(disposable);
			});
		}
	}
}
