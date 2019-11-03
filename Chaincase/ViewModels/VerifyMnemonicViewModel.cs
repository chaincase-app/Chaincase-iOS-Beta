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
		private string _mnemonicString { get; }
		private string[] _mnemonicWords { get; }

		private string[] _recall;
		public string[] Recall
		{
			get => _recall;
			set => this.RaiseAndSetIfChanged(ref _recall, value);
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

			IsVerified = string.Equals(Recall[0], _mnemonicWords[0], StringComparison.CurrentCultureIgnoreCase) &&
				string.Equals(Recall[1], _mnemonicWords[3], StringComparison.CurrentCultureIgnoreCase) &&
				string.Equals(Recall[2], _mnemonicWords[6], StringComparison.CurrentCultureIgnoreCase) &&
				string.Equals(Recall[3], _mnemonicWords[9], StringComparison.CurrentCultureIgnoreCase) &&
				WalletController.VerifyWalletCredentials(_mnemonicString, _passphrase, Global.Network);
			if (!IsVerified) return;
			WalletController.LoadWalletAsync(Global.Network);
			NavMainCommand.Execute();
		}
	}
}
