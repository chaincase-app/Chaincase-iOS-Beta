using System;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Navigation;
using ReactiveUI;
using Splat;

namespace Chaincase.ViewModels
{
    public class SentViewModel : ViewModelBase
    {
        public SentViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
        {
            NavWalletCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                ViewStackService.PushPage(new MainViewModel(), null, true).Subscribe();
                return Observable.Return(Unit.Default);
            });
        }

        public ReactiveCommand<Unit, Unit> NavWalletCommand;
    }
}
