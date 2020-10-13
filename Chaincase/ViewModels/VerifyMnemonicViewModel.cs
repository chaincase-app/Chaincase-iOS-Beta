using System;
using Chaincase.Navigation;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using Splat;
using NBitcoin;
using WalletWasabi.Blockchain.Keys;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Chaincase.ViewModels
{
	public class VerifyMnemonicViewModel : ViewModelBase
	{
		protected Global Global { get; }

        // <param name="isFinal">
        // we verify 2 words, isFinal -> we verified 1 word already
        // </param>
		public VerifyMnemonicViewModel(List<string> seedWords, string previouslyVerified)
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();
			SeedWords = seedWords;

            while (WordToVerify == null || WordToVerify == previouslyVerified)
            {
			    // "random" check words were written. no need for CSPRNG
			    WordToVerify = seedWords.RandomElement();
            }

			IndexToVerify = seedWords.IndexOf(WordToVerify);

			VerifiedCommand = ReactiveCommand.CreateFromObservable(() =>
			{
                if (previouslyVerified != null)
                {
					Global.UiConfig.IsBackedUp = true;
					Global.UiConfig.ToFile(); // successfully backed up!
					ViewStackService.PopPage(false);  // this
					ViewStackService.PopPage(false); // previouslyVerified
					ViewStackService.PopPage(false);
					ViewStackService.PopPage();     // words
                } else
                {
					ViewStackService.PushPage(new VerifyMnemonicViewModel(seedWords, WordToVerify)).Subscribe();
                }
				return Observable.Return(Unit.Default);
			});

			FailedCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				ViewStackService.PopPage(false);  // this
                if (previouslyVerified != null)
				    ViewStackService.PopPage(false);  // previouslyVerified
				return Observable.Return(Unit.Default);
			});
		}

		public List<string> SeedWords { get; }
		public string WordToVerify;
		public int IndexToVerify;
		public ReactiveCommand<Unit, Unit> VerifiedCommand;
        public ReactiveCommand<Unit, Unit> FailedCommand;

	}
}
