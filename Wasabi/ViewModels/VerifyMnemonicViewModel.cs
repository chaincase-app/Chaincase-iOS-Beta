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

		public ICommand VerifyCommand => new Command(async () => await VerifyMnemonicAsync());

		private async Task VerifyMnemonicAsync()
		{
			await Task.Run(() =>
			{
				System.Diagnostics.Debug.WriteLine(string.Join(" ", Recall).ToString());

				IsVerified = Recall[0] == MnemonicWords[0] &&
					Recall[1] == MnemonicWords[3] &&
					Recall[2] == MnemonicWords[6] &&
					Recall[3] == MnemonicWords[9] &&
					GenerateWalletController.VerifyWalletCredentials(MnemonicString, _passphrase);
			});
		}
	}
}
