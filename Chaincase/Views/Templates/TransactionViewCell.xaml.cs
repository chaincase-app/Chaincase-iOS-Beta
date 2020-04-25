using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;

namespace Chaincase.Views.Templates
{
    public partial class TransactionViewCell : ReactiveViewCell<TransactionViewModel>
	{
		public TransactionViewCell()
		{
			InitializeComponent();

			this.WhenActivated(d =>
			{
				this.OneWayBind(ViewModel,
					vm => vm.DateTime,
					v => v.DateTime.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
	                vm => vm.AmountBtc,
	                v => v.AmountBtc.Text)
	                .DisposeWith(d);
				this.OneWayBind(ViewModel,
	                vm => vm.TransactionId,
	                v => v.TransactionId.Text)
	                .DisposeWith(d);
			});
			
		}
	}
}
