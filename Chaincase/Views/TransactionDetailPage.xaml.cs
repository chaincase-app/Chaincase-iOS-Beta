using System;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using System.Reactive.Disposables;
using Xamarin.Forms;

namespace Chaincase.Views
{
	public partial class TransactionDetailPage : ReactiveContentPage<TransactionViewModel>
	{
		public TransactionDetailPage()
		{
			InitializeComponent();
			this.WhenActivated(d =>
			{
				this.OneWayBind(ViewModel,
					vm => vm.DateTime,
					v => v.Date.Text)
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.Confirmed,
					v => v.Confirmed.Text,
					isC => isC ? "Confirmed" : "waiting for confirmation")
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
