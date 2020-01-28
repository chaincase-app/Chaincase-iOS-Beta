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

			this.WhenActivated(d =>
			{
				this.OneWayBind(ViewModel,
								vm => vm.Amount,
								v => v.Amount.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
                    vm => vm.AnonymitySet,
                    v => v.AnonymitySet.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
                    vm => vm.Clusters,
                    v => v.Clusters.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.Status,
					v => v.Status.Text,
					vmToViewConverterOverride: new CoinStatusStringConverter())
					.DisposeWith(d);
			});
		}
	}
}
