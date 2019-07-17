using System.Windows.Input;
using Xamarin.Forms;
using Wasabi.Navigation;
using System.Threading.Tasks;
using Wasabi.Controllers;

namespace Wasabi.ViewModels
{
	public class PassphraseViewModel : ViewModelBase
	{

		public PassphraseViewModel(INavigationService navigationService) : base(navigationService)
		{
		}
		private string _passphrase { get; set; }
		public string Passphrase
		{
			get
			{
				return _passphrase;
			}
			set
			{
				_passphrase = value;
				RaisePropertyChanged(() => Passphrase);
			}
		}

		public ICommand SubmitCommand => new Command(async () => await PushMnemonicAsync());

		private async Task PushMnemonicAsync()
		{
			await _navigationService.NavigateAsync(
				"MnemonicPage",
				WalletController.GenerateMnemonicAsync(_passphrase).Result.ToString());
		}
	}
}
