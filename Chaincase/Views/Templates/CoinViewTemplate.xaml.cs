using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;

namespace Chaincase.Views.Templates
{
	public partial class CoinViewTemplate : ReactiveContentView<CoinViewModel>
	{
		public CoinViewTemplate()
		{
			InitializeComponent();

			this.WhenActivated(disposable =>
			{
				this.Bind(ViewModel,
								vm => vm.IsSelected,
								v => v.IsSelected.IsToggled)
					.DisposeWith(disposable);
				this.OneWayBind(ViewModel,
								x => x.Amount,
								x => x.Amount.Text)
					.DisposeWith(disposable);
				this.OneWayBind(ViewModel, x => x.AnonymitySet, x => x.AnonymitySet.Text)
					.DisposeWith(disposable);
				this.OneWayBind(ViewModel, x => x.Clusters, x => x.Clusters.Text)
					.DisposeWith(disposable);
			});
		}
	}
}
