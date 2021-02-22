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

        public void SetIsBackedUp()
        {
            Global.UiConfig.IsBackedUp = true;
            Global.UiConfig.ToFile(); // successfully backed up!
        }

        public List<string> SeedWords
        {
            get => _seedWords;
            set => this.RaiseAndSetIfChanged(ref _seedWords, value);
        }
    }
}
