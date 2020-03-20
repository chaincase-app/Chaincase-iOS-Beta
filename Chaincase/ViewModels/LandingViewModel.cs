using System;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Navigation;
using ReactiveUI;

namespace Chaincase.ViewModels
{
	public class LandingViewModel : BaseViewModel
	{
		public LandingViewModel(IViewStackService viewStackService) : base(viewStackService)
		{
			NewWalletCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				viewStackService.PushPage(new PassphraseViewModel(viewStackService)).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		public ReactiveCommand<Unit, Unit> NewWalletCommand;
		public ReactiveCommand<Unit, Unit> RecoverWalletCommand;

	}
}
