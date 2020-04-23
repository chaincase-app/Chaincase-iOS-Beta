using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System;
using Chaincase.Controllers;
using Chaincase.Navigation;
using Splat;

namespace Chaincase.ViewModels
{
	public class PasswordViewModel : ViewModelBase
	{
		public ReactiveCommand<Unit, Unit> SubmitCommand;

		public PasswordViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			SubmitCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				var mnemonic = WalletController.GenerateMnemonic(Password, Global.Network).ToString();
				ViewStackService.PushPage(new MnemonicViewModel(mnemonic)).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		private string _password;
		public string Password
		{
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
		}
	}
}
