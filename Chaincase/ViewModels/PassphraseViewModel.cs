using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System;
using Chaincase.Controllers;
using Chaincase.Navigation;
using Splat;

namespace Chaincase.ViewModels
{
	public class PassphraseViewModel : ViewModelBase
	{
		public ReactiveCommand<Unit, Unit> SubmitCommand;

		public PassphraseViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			SubmitCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				var mnemonic = WalletController.GenerateMnemonic(Passphrase, Global.Network).ToString();
				ViewStackService.PushPage(new MnemonicViewModel(mnemonic)).Subscribe();
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
