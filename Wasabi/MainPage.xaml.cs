using NBitcoin;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.KeyManagement;
using WalletWasabi.Logging;
using Xamarin.Forms;

namespace Wasabi
{
	public partial class MainPage : ContentPage
	{
		private string _passphrase;

		public string Passphrase
		{
			get => _passphrase;
			set
			{
				if (value == _passphrase) return;
				_passphrase = value;
				OnPropertyChanged(nameof(Passphrase));
			}
		}

		public MainPage()
		{
			InitializeComponent();
			
		}

		async void OnSubmitButtonClickedAsync(object sender, EventArgs e)
		{
			string walletFilePath = Path.Combine(Global.WalletsDir, $"Main.json");

			try
			{
				KeyManager.CreateNew(out Mnemonic mnemonic, Passphrase, walletFilePath);

				await Navigation.PushAsync(new MnemonicPage
				{
					BindingContext = new { Text = mnemonic.ToString() }
				});
			}
			catch (Exception ex)
			{
				Logger.LogError<KeyManager>(ex);
			}
		}

		async void OnNextButtonClickedAsync(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new MnemonicPage());
		}
	}
}
