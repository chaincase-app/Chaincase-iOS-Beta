using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using Chaincase.Converters;
using NBitcoin;

namespace Chaincase.Views.Templates
{
	public partial class SeedWordTemplate : ReactiveContentView<SeedWordViewModel>
	{
		public SeedWordTemplate()
		{
			InitializeComponent();

			this.WhenActivated(d =>
			{
				this.OneWayBind(ViewModel,
					vm => vm.Word,
					v => v.WordLabel.Text)
					.DisposeWith(d);
			});
		}
	}
}
