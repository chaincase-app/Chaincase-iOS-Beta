using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace Chaincase.Views
{
	public partial class AddressPage : ReactiveContentPage<AddressViewModel>
	{

		public AddressPage()
		{
			InitializeComponent();

			this.WhenActivated(d =>
			{
				this.Bind(ViewModel,
					vm => vm.Memo,
					v => v.Memo.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.BitcoinUri,
					v => v.QrCode.BarcodeValue)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.Address,
					v => v.Address.Text)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.BitcoinUri,
					v => v.BitcoinUri.Text)
						.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.RequestAmountCommand,
					v => v.RequestAmountButton)
					.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.ShareCommand,
					v => v.ShareButton,
                    vm => vm.BitcoinUri)
					.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.NavWalletCommand,
					v => v.WalletButton)
					 .DisposeWith(d);
			});

			var addressGestureRecognizer = new TapGestureRecognizer();
			addressGestureRecognizer.Tapped += async (s, e) => {
				Clipboard.SetTextAsync(Address.Text);
				if (Clipboard.HasText)
				{
					var text = await Clipboard.GetTextAsync();
					DisplayAlert("Success", string.Format("Copied address to clipboard", text), "OK");
				}
			};
			Address.GestureRecognizers.Add(addressGestureRecognizer);

			var bitcoinUriGestureRecognizer = new TapGestureRecognizer();
			bitcoinUriGestureRecognizer.Tapped += async (s, e) => {
				Clipboard.SetTextAsync(BitcoinUri.Text);
				if (Clipboard.HasText)
				{
					var text = await Clipboard.GetTextAsync();
					DisplayAlert("Success", string.Format("Copied bitcoin URI to clipboard", text), "OK");
				}
			};
			BitcoinUri.GestureRecognizers.Add(bitcoinUriGestureRecognizer);
		}
	}
}
