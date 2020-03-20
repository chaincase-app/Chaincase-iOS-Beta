using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System;
using Chaincase.Controllers;
using Chaincase.Navigation;

namespace Chaincase.ViewModels
{
	public class PassphraseViewModel : BaseViewModel
	{
		public ReactiveCommand<Unit, Unit> SubmitCommand;

		public PassphraseViewModel(IViewStackService viewStackService) : base(viewStackService)
		{
			SubmitCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				var mnemonic = WalletController.GenerateMnemonic(Passphrase, Global.Network).ToString();
				viewStackService.PushPage(new MnemonicViewModel(viewStackService, mnemonic)).Subscribe();
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
