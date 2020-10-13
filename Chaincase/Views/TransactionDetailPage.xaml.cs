using System;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using System.Reactive.Disposables;
using Xamarin.Forms;
using Xamarin.Essentials;

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
					vm => vm.AmountBtc,
					v => v.AmountBtc.Text,
					amt => $"Amount: {amt}")
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.TransactionId,
					v => v.TransactionId.Text)
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.DateTime,
					v => v.Date.Text,
					d => $"Date: {d}")
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.BlockHeight,
					v => v.BlockHeight.Text,
					bh => $"Block Height: {bh}")
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.Confirmed,
					v => v.Confirmed.Text,
					isC => isC ? "Confirmed ✔" : "Waiting for confirmation... ")
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.Confirmations,
					v => v.Confirmations.Text,
					cs => $"Confirmations: {cs}")
					.DisposeWith(d);
			});

			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += async (s, e) => {
				Clipboard.SetTextAsync(TransactionId.Text);
				if (Clipboard.HasText)
				{
					var text = await Clipboard.GetTextAsync();
					DisplayAlert("Success", string.Format("Copied to clipboard", text), "OK");
				}
			};
			TransactionId.GestureRecognizers.Add(tapGestureRecognizer);
		}
	}
}
