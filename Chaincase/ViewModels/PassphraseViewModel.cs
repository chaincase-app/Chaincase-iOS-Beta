using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System;
using Chaincase.Controllers;

namespace Chaincase.ViewModels
{
	public class PassphraseViewModel : ViewModelBase
	{
		public ReactiveCommand<Unit, Unit> SubmitCommand;

		public PassphraseViewModel(IScreen hostScreen) : base(hostScreen)
		{
			SubmitCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				var mnemonic = WalletController.GenerateMnemonic(Passphrase, Global.Network).ToString();
				HostScreen.Router.Navigate.Execute(new MnemonicViewModel(hostScreen, mnemonic)).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		private string _passphrase;
		public string Passphrase
		{
			get => _passphrase;
			set => this.RaiseAndSetIfChanged(ref _passphrase, value);
		}
	}
}
