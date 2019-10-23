using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Chaincase.Navigation;
using Chaincase.Controllers;
using Xamarin.Forms;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace Chaincase.ViewModels
{
	public class VerifyMnemonicViewModel : ViewModelBase
	{
		public string MnemonicString { get; }
		public string[] MnemonicWords { get; }
		public string[] Recall { get; set; }

        //Recall words
        public string Recall0 
        {
            get => Recall[0];
            set => this.RaiseAndSetIfChanged(ref Recall[0], value);
        }

        public string Recall1
        {
            get => Recall[1];
            set => this.RaiseAndSetIfChanged(ref Recall[1], value);
        }

        public string Recall2
        {
            get => Recall[2];
            set => this.RaiseAndSetIfChanged(ref Recall[2], value);
        }

        public string Recall3
        {
            get => Recall[3];
            set => this.RaiseAndSetIfChanged(ref Recall[3], value);
        }

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

			NavMainCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				HostScreen.Router.Navigate.Execute(new MainViewModel(hostScreen)).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		public ICommand TryCompleteInitializationCommand => new Command(async () => await TryCompleteInitializationAsync());
		public ReactiveCommand<Unit, Unit> NavMainCommand;

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
			NavMainCommand.Execute();
		}
	}
}
