using System;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Controllers;
using Chaincase.Navigation;
using ReactiveUI;
using Splat;

namespace Chaincase.ViewModels
{
	public class PasswordPromptViewModel : ViewModelBase
	{
		private string _password;
		private string _acceptText;

		public PasswordPromptViewModel(string acceptText = "Accept")
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Password = "";
			ValidatePasswordCommand = ReactiveCommand.CreateFromObservable(ValidatePassword);

			CancelCommand = ReactiveCommand.CreateFromObservable(ViewStackService.PopModal);
			_acceptText = acceptText;
		}

        // subscribe to this function after this model is made from within
        // the calling/pushing viewmodel
        public IObservable<string> ValidatePassword()
        {
			return Observable.Start(() => WalletController.IsValidPassword(Password, Global.Network));
        }

		public ReactiveCommand<Unit, string> ValidatePasswordCommand;
		public ReactiveCommand<Unit, Unit> CancelCommand;

		public string AcceptText
		{
			get => _acceptText;
			set => this.RaiseAndSetIfChanged(ref _acceptText, value);
		}

		public string Password
        {
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
        }
	}
}
