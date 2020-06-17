using System;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using System.Reactive.Disposables;
using Xamarin.Forms;
using Xamarin.Essentials;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.PlatformConfiguration;
using Splat;
using Chaincase.Navigation;

namespace Chaincase.Views
{
	public partial class CoinDetailModal : ReactiveContentPage<CoinViewModel>
	{
		public CoinDetailModal()
		{
			On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FormSheet);
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
					vm => vm.OutputIndex,
					v => v.OutputIndex.Text,
					oi => $"Output Index: {oi}")
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.Confirmations,
					v => v.Confirmations.Text,
					cs => $"Confirmations: {cs}")
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.AnonymitySet,
					v => v.AnonymitySet.Text,
					aSet => $"Anonymity Set: {aSet}")
				.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.Clusters,
					v => v.Clusters.Text,
					cs => $"Clusters: {cs}")
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

		protected override async void OnDisappearing()
		{
			base.OnDisappearing();
			Locator.Current.GetService<IViewStackService>().PopModal();
		}
	}
}
