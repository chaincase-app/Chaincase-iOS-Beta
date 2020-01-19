using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;

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
					vm => vm.Coins,
					v => v.Coins.ItemsSource)
					.DisposeWith(d);
			});
			
		}
	}
}
