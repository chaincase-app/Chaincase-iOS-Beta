using System.Windows.Input;
using Xamarin.Forms;
using Wasabi.Navigation;
using System.Threading.Tasks;

namespace Wasabi.ViewModels
{
	public class MnemonicViewModel : ViewModelBase
	{
		private string _mnemonicString;
		public string MnemonicString
		{
			get
			{
				return _mnemonicString;
			}
			set
			{
				_mnemonicString = value;
				RaisePropertyChanged(() => MnemonicString);
			}
		}

		public MnemonicViewModel(INavigationService navigationService, string mnemonicString) : base(navigationService)
		{
			MnemonicString = mnemonicString;
		}

		public ICommand AcceptCommand => new Command(async () => await AcceptMnemonicAsync());

		private async Task AcceptMnemonicAsync()
		{
			await _navigationService.NavigateAsync("VerifyMnemonicPage", MnemonicString);
		}
	}
}
