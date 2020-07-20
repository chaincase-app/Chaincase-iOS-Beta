using System;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Navigation;
using ReactiveUI;
using Splat;

namespace Chaincase.ViewModels
{
	public class LandingViewModel : ViewModelBase
	{
		public LandingViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			NewWalletCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				ViewStackService.PushPage(new NewPasswordViewModel()).Subscribe();
				return Observable.Return(Unit.Default);
			});

			LoadWalletCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				ViewStackService.PushPage(new LoadWalletViewModel()).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		public ReactiveCommand<Unit, Unit> NewWalletCommand;
		public ReactiveCommand<Unit, Unit> LoadWalletCommand;

	}
}
