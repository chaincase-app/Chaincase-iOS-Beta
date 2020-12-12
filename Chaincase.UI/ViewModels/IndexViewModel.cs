using System.Reactive.Disposables;
using Chaincase.Common;
using ReactiveUI;
using Splat;

namespace Chaincse.UI.ViewModels
{
	public class IndexViewModel : ReactiveObject
    {
        protected Global Global { get; }

        private CompositeDisposable Disposables { get; set; }
        public string _balance;
        private ObservableAsPropertyHelper<bool> _hasCoins;
        private ObservableAsPropertyHelper<bool> _hasSeed;
        private ObservableAsPropertyHelper<bool> _isBackedUp;
        private ObservableAsPropertyHelper<bool> _canBackUp;
        private bool _hasPrivateCoins;
        readonly ObservableAsPropertyHelper<bool> _isJoining;

        public IndexViewModel()
        {
            Global = Locator.Current.GetService<Global>();
        }

    }
}
