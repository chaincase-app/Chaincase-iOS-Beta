using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using ReactiveUI;
using Splat;

namespace Chaincase.UI.ViewModels
{
    public class BackUpViewModel : ReactiveObject
    {
        protected Global Global { get; }
        protected IHsmStorage HSM { get; }

        private List<string> _seedWords;
        private string _wordToVerify;

        public BackUpViewModel(Global global, IHsmStorage hsm)
        {
            Global = global;
            HSM = hsm;
        }

        public async Task<bool> HasGotSeedWords()
        {
            var seedWords = await HSM.GetAsync($"{Global.Network}-seedWords");
            if (seedWords is null) return false;

            SeedWords = seedWords.Split(' ').ToList();
            return true;
        }

        public void ChangeWordToVerify(string previousWord)
		{
            var word = WordToVerify;
            while (word == previousWord)
			{
                word = SeedWords.RandomElement();
			}
            WordToVerify = word;
		}

        public List<string> SeedWords
        {
            get => _seedWords;
            set => this.RaiseAndSetIfChanged(ref _seedWords, value);
        }

        public string WordToVerify
        {
            get => _wordToVerify;
            set => this.RaiseAndSetIfChanged(ref _wordToVerify, value);
        }
    }
}
