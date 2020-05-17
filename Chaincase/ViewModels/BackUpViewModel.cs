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

		private IEnumerable<string> _seedWords;

		public BackUpViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			SeedWords = new string[] { "a", "b", "c" };

			NextCommand = ReactiveCommand.CreateFromObservable(ViewStackService.PopModal);
		}

		public ReactiveCommand<Unit, Unit> NextCommand;

        public IEnumerable<string> SeedWords
        {
			get => _seedWords;
			set => this.RaiseAndSetIfChanged(ref _seedWords, value);
        }
	}
}
