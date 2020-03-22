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

		public PasswordPromptViewModel(ReactiveCommand<string, bool> commandRequiringPassword)
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Password = "";
			Cancel = ReactiveCommand.CreateFromObservable(ViewStackService.PopModal);
			CommandRequiringPassword = ReactiveCommand.Create(() =>
			{
			    commandRequiringPassword.Execute(Password).Subscribe(succ =>
				{
					if (succ) ViewStackService.PopModal().Subscribe();
				});
			});
		}

		public ReactiveCommand<Unit, Unit> CommandRequiringPassword;
		public ReactiveCommand<Unit, Unit> Cancel;
		
		public string Password
        {
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
        }
	}
}
