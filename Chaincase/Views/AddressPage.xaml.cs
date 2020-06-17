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
				this.Bind(ViewModel,
					vm => vm.Address,
					v => v.Address.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.Address,
					v => v.QrCode.BarcodeValue,
					AddressToBitcoinUrl)
					.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.ShareCommand,
					v => v.ShareButton,
                    vm => vm.Address)
					.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.NavWalletCommand,
					v => v.WalletButton)
					 .DisposeWith(d);
			});

			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += async (s, e) => {
				Clipboard.SetTextAsync(Address.Text);
				if (Clipboard.HasText)
				{
					var text = await Clipboard.GetTextAsync();
					DisplayAlert("Success", string.Format("Copied to clipboard", text), "OK");
				}
			};
			Address.GestureRecognizers.Add(tapGestureRecognizer);
		}

        public string AddressToBitcoinUrl(string address)
        {
			return "bitcoin:" + address;
        }
	}
}
