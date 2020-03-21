using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Navigation;
using ReactiveUI;
using Splat;
using WalletWasabi.Helpers;

namespace Chaincase.ViewModels
{
	public class PasswordPromptViewModel : ViewModelBase
	{

		private readonly Interaction<string, bool> _accept;

		public Interaction<string, bool> Accept => _accept;

		private string _password;

		public PasswordPromptViewModel(ReactiveCommand<string, bool> commandRequiringPassword)
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Password = "";
			Cancel = ReactiveCommand.CreateFromObservable(ViewStackService.PopModal);
			CommandRequiringPassword = commandRequiringPassword;
		}

		public ReactiveCommand<string, bool> CommandRequiringPassword;
		public ReactiveCommand<Unit, Unit> Cancel;
		
		public string Password
        {
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
        }
	}
}
