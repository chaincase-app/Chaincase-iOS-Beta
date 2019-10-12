using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Wasabi.ViewModels;

namespace Wasabi.Views.Templates
{
	public partial class CoinViewTemplate : ReactiveContentView<CoinViewModel>
	{
		public CoinViewTemplate()
		{
			InitializeComponent();

			this.WhenActivated(disposable =>
			{
				this.OneWayBind(ViewModel, x => x.Amount, x => x.Amount.Text)
					.DisposeWith(disposable);
				this.OneWayBind(ViewModel, x => x.AnonymitySet, x => x.AnonymitySet.Text)
					.DisposeWith(disposable);
				this.OneWayBind(ViewModel, x => x.Clusters, x => x.Clusters.Text)
					.DisposeWith(disposable);
			});
		}
	}
}
