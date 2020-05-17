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
		protected Global Global { get; }

		private string _password;
		private string _acceptText;

		public StartBackUpViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			NextCommand = ReactiveCommand.CreateFromObservable(ViewStackService.PopModal);
		}

		public ReactiveCommand<Unit, Unit> NextCommand;
	}
}
