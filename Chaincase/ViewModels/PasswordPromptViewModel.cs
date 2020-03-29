using System;
using System.Reactive;
using Chaincase.Navigation;
using ReactiveUI;
using Splat;

namespace Chaincase.ViewModels
{
	public class PasswordPromptViewModel : ViewModelBase
	{
		private string _password;
		private string _acceptText;

		public PasswordPromptViewModel(ReactiveCommand<string, bool> commandRequiringPassword, string acceptText = "Accept")
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Password = "";
			CommandRequiringPassword = ReactiveCommand.Create(() =>
			{
			    commandRequiringPassword.Execute(Password).Subscribe(succ =>
				{
					if (succ) ViewStackService.PopModal().Subscribe();
				});
			});
			Cancel = ReactiveCommand.CreateFromObservable(ViewStackService.PopModal);
			_acceptText = acceptText;
		}

		public ReactiveCommand<Unit, Unit> CommandRequiringPassword;
		public ReactiveCommand<Unit, Unit> Cancel;

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
