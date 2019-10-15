using System.Windows.Input;
using Xamarin.Forms;
using Chaincase.Navigation;
using System.Threading.Tasks;
using Chaincase.Controllers;
using ReactiveUI;

namespace Chaincase.ViewModels
{
	public class PassphraseViewModel : ViewModelBase
	{

		public PassphraseViewModel(IScreen hostScreen) : base(hostScreen)
		{
		}
		private string _passphrase;
		public string Passphrase
		{
			get => _passphrase;
			set => this.RaiseAndSetIfChanged(ref _passphrase, value);
		}

		//public ICommand SubmitCommand => new Command(async () => await PushMnemonicAsync());

		//private async Task PushMnemonicAsync()
		//{
		//	await _navigationService.NavigateTo( new
		//		MnemonicViewModel(_navigationService,
		//		WalletController.GenerateMnemonic(_passphrase, Global.Network).ToString()));
		//}
	}
}
