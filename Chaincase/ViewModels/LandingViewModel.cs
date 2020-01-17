using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace Chaincase.ViewModels
{
	public class LandingViewModel : ViewModelBase
	{
		public LandingViewModel(IScreen hostScreen) : base(hostScreen)
		{
			NewWalletCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				HostScreen.Router.Navigate.Execute(new PassphraseViewModel(hostScreen)).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		public ReactiveCommand<Unit, Unit> NewWalletCommand;
		public ReactiveCommand<Unit, Unit> RecoverWalletCommand;

	}
}
