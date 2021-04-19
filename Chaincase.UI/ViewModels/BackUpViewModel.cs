using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using ReactiveUI;

namespace Chaincase.UI.ViewModels
{
    public class BackUpViewModel : ReactiveObject
    {
        private Config Config { get; }
        private UiConfig UiConfig { get; }
        protected IHsmStorage HSM { get; }

        private List<string> _seedWords;

        public BackUpViewModel(Config config, UiConfig uiConfig, IHsmStorage hsm)
        {
            Config = config;
            UiConfig = uiConfig;
            HSM = hsm;
        }

        public async Task<bool> HasGotSeedWords()
        {
            var seedWords = await HSM.GetAsync($"{Config.Network}-seedWords");
            if (seedWords is null) return false;

            SeedWords = seedWords.Split(' ').ToList();
            return true;
        }

        public void SetIsBackedUp()
        {
            UiConfig.IsBackedUp = true;
            UiConfig.ToFile(); // successfully backed up!
        }

        public List<string> SeedWords
        {
            get => _seedWords;
            set => this.RaiseAndSetIfChanged(ref _seedWords, value);
        }
    }
}
