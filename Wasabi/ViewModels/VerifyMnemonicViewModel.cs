using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Wasabi.Navigation;
using Wasabi.Controllers;
using Xamarin.Forms;
using ReactiveUI;

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
			get => _isVerified;
			set => this.RaiseAndSetIfChanged(ref _isVerified, value);
		}

		private string _passphrase;
		public string Passphrase
		{
			get => _passphrase;
			set => this.RaiseAndSetIfChanged(ref _passphrase, value);
		}

		public VerifyMnemonicViewModel(IScreen hostScreen, string mnemonicString) : base(hostScreen)
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
				WalletController.VerifyWalletCredentials(MnemonicString, _passphrase, Global.Network);
			if (!IsVerified) return;
			WalletController.LoadWalletAsync(Global.Network);
			//await NavigateToMain();
		}

		//public ICommand NavCommand => new Command(async () => await NavigateToMain());

		//private async Task NavigateToMain()
		//{
		//	await _navigationService.NavigateTo(new MainViewModel(_navigationService));
		//}
	}
}
