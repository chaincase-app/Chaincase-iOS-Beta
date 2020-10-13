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
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Chaincase.ViewModels
{
	public class BackUpViewModel : ViewModelBase
	{
		protected Global Global { get; }

		private List<string> _seedWords;
        private string[] _indexedWords;


		public BackUpViewModel(List<string> seedWords)
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();
			SeedWords = seedWords;

			IndexedWords = new string[SeedWords.Count()];
			for (int i = 0; i < SeedWords.Count(); i++)
            {
				IndexedWords[i] = $"{i+1}. {SeedWords[i]}";
            }

			VerifyCommand = ReactiveCommand.CreateFromObservable(() =>
			{
                if (Global.UiConfig.IsBackedUp)
                {
					// pop back home
					ViewStackService.PopPage();
					ViewStackService.PopPage();
                } else
                {
					// verify backup
					ViewStackService.PushPage(new VerifyMnemonicViewModel(SeedWords, null)).Subscribe();
                }
				return Observable.Return(Unit.Default);
			});
		}

		public ReactiveCommand<Unit, Unit> VerifyCommand;

		public List<string> SeedWords
        {
			get => _seedWords;
			set => this.RaiseAndSetIfChanged(ref _seedWords, value);
        }

		public string[] IndexedWords
		{
			get => _indexedWords;
			set => this.RaiseAndSetIfChanged(ref _indexedWords, value);
		}
	}
}
