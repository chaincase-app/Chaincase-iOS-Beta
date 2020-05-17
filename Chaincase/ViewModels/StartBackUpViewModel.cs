using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase.ViewModels
{
	public class StartBackUpViewModel : ViewModelBase
	{
		public StartBackUpViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			NextCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				ViewStackService.PushModal(new BackUpViewModel()).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		public ReactiveCommand<Unit, Unit> NextCommand;
	}
}
