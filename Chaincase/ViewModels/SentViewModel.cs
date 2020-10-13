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
                ViewStackService.PopPage(false);
                ViewStackService.PopPage(false);
                return ViewStackService.PopPage(true);
            });
        }

        public ReactiveCommand<Unit, Unit> NavWalletCommand;
    }
}
