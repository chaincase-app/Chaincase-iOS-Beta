using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Wasabi.Navigation;
using Wasabi.Controllers;
using Xamarin.Forms;

namespace Wasabi.ViewModels
{
	public class VerifyMnemonicViewModel : ViewModelBase
	{
		public string MnemonicString { get; }
		public string[] MnemonicWords { get; }
		public string[] Recall { get; }
		private bool _isVerified;
		public bool IsVerified
		{
			get
			{
				return _isVerified;
			}
			set
			{
				_isVerified = value;
				RaisePropertyChanged(() => IsVerified);
			}
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

		public VerifyMnemonicViewModel(INavigationService navigationService, string mnemonicString) : base(navigationService)
		{
			MnemonicString = mnemonicString;
			MnemonicWords = mnemonicString.Split(" ");
			Recall = new string[4];
			IsVerified = false;
		}

		public ICommand TryCompleteInitializationCommand => new Command(async () => await TryCompleteInitializationAsync());

		private async Task TryCompleteInitializationAsync()
		{
			System.Diagnostics.Debug.WriteLine(string.Join(" ", Recall));

			IsVerified = string.Equals(Recall[0], MnemonicWords[0], StringComparison.CurrentCultureIgnoreCase) &&
				string.Equals(Recall[1], MnemonicWords[3], StringComparison.CurrentCultureIgnoreCase) &&
				string.Equals(Recall[2], MnemonicWords[6], StringComparison.CurrentCultureIgnoreCase) &&
				string.Equals(Recall[3], MnemonicWords[9], StringComparison.CurrentCultureIgnoreCase) &&
				WalletController.VerifyWalletCredentials(MnemonicString, _passphrase);
			if (!IsVerified) return;
			WalletController.LoadWalletAsync();
			await NavigateToMain();
		}

		public ICommand NavCommand => new Command(async () => await NavigateToMain());

		private async Task NavigateToMain()
		{
			await _navigationService.NavigateAsync("MainPage");
		}
	}
}
