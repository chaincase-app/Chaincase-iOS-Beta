using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;
using Xamarin.Essentials;

namespace Chaincase.ViewModels
{
	public class BackUpViewModel : ViewModelBase
	{
		protected Global Global { get; }

		private List<SeedWordViewModel> _seedWords;

		public BackUpViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			SeedWords = new List<SeedWordViewModel> { new SeedWordViewModel("a", 1), new SeedWordViewModel("b", 2)};
            
			VerifyCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				ViewStackService.PushModal(new StartBackUpViewModel()).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		public ReactiveCommand<Unit, Unit> VerifyCommand;


		public List<SeedWordViewModel> SeedWords
        {
			get => _seedWords;
			set => this.RaiseAndSetIfChanged(ref _seedWords, value);
        }
	}
}
