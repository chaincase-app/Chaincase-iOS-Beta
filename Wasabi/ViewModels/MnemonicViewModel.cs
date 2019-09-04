using System.Windows.Input;
using Xamarin.Forms;
using Wasabi.Navigation;
using System.Threading.Tasks;
using ReactiveUI;

namespace Wasabi.ViewModels
{
	public class MnemonicViewModel : ViewModelBase
	{
		private string _mnemonicString;
		public string MnemonicString
		{
			get => _mnemonicString;
			set => this.RaiseAndSetIfChanged(ref _mnemonicString, value);
		}

		public MnemonicViewModel(IScreen hostScreen, string mnemonicString) : base(hostScreen)
		{
			MnemonicString = mnemonicString;
		}

		//public ICommand AcceptCommand => new Command(async () => await AcceptMnemonicAsync());

		//private async Task AcceptMnemonicAsync()
		//{
		//	await _navigationService.NavigateTo(new VerifyMnemonicViewModel(_navigationService, MnemonicString));
		//}
	}
}
