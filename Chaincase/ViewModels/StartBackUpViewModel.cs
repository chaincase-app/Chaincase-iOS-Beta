using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;
using Xamarin.Forms;

namespace Chaincase.ViewModels
{
	public class StartBackUpViewModel : ViewModelBase
	{
		protected Global Global { get; }

		public StartBackUpViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();
			var hsm = DependencyService.Get<IHsmStorage>();

			NextCommand = ReactiveCommand.CreateFromObservable(() =>
			{

				List<string> seedWords = hsm.GetAsync($"{Global.Network}-seedWords").Result?.Split(' ').ToList();
				if (seedWords != null)
				{
					ViewStackService.PushPage(new BackUpViewModel(seedWords)).Subscribe();
				}
				return Observable.Return(Unit.Default);
			});
		}

		public ReactiveCommand<Unit, Unit> NextCommand;
	}
}
