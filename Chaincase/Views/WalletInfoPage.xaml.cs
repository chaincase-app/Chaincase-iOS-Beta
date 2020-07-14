using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace Chaincase.Views
{
	public partial class WalletInfoPage : ReactiveContentPage<WalletInfoViewModel>
	{

		public WalletInfoPage()
		{
			InitializeComponent();

			this.WhenActivated(d =>
			{
				this.Bind(ViewModel,
					vm => vm.AccountKeyPath,
					v => v.AccountKeyPathLabel.Text)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.ExtendedAccountPublicKey,
					v => v.ExtendedAccountPublicKeyLabel.Text)
					.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.ShareLogsCommand,
					v => v.ShareLogsButton)
                    .DisposeWith(d);
			});

			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += async (s, e) => {
				await Clipboard.SetTextAsync(((Label)s).Text);
				if (Clipboard.HasText)
				{
					var text = await Clipboard.GetTextAsync();
					DisplayAlert("Success", string.Format("Copied xpub to clipboard", text), "OK");
				}
			};
			ExtendedAccountPublicKeyLabel.GestureRecognizers.Add(tapGestureRecognizer);
		}
	}
}
