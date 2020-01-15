using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Chaincase.Navigation;
using Chaincase.Controllers;
using Xamarin.Forms;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;

namespace Chaincase.ViewModels
{
	public class VerifyMnemonicViewModel : ViewModelBase
	{
		private string _mnemonicString { get; }
		private string[] _mnemonicWords { get; }

		private string _recall0;
		public string Recall0
		{
			get => _recall0;
			set => this.RaiseAndSetIfChanged(ref _recall0, value);
		}

		private string _recall1;
		public string Recall1
		{
			get => _recall1;
			set => this.RaiseAndSetIfChanged(ref _recall1, value);
		}
		private string _recall2;
		public string Recall2
		{
			get => _recall2;
			set => this.RaiseAndSetIfChanged(ref _recall2, value);
		}
		private string _recall3;
		public string Recall3
		{
			get => _recall3;
			set => this.RaiseAndSetIfChanged(ref _recall3, value);
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
			_mnemonicString = mnemonicString;
			_mnemonicWords = mnemonicString.Split(" ");
			Recall0 = Recall1 = Recall2 = Recall3 = "";
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
			IsVerified = string.Equals(Recall0, _mnemonicWords[0], StringComparison.CurrentCultureIgnoreCase) &&
				         string.Equals(Recall1, _mnemonicWords[3], StringComparison.CurrentCultureIgnoreCase) &&
				         string.Equals(Recall2, _mnemonicWords[6], StringComparison.CurrentCultureIgnoreCase) &&
				         string.Equals(Recall3, _mnemonicWords[9], StringComparison.CurrentCultureIgnoreCase) &&
				         WalletController.VerifyWalletCredentials(_mnemonicString, _passphrase, Global.Network);
			if (!IsVerified) return;
			WalletController.LoadWalletAsync(Global.Network);
			NavMainCommand.Execute();
		}
	}
}
