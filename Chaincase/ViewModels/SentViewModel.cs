using System;
using System.Reactive;
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
            NavWalletCommand = ReactiveCommand.CreateFromObservable<Unit, Unit>(NavMain);
        }

        public IObservable<Unit> NavMain(Unit _)
        {
            return ViewStackService.PushPage(new MainViewModel(), "mainViewModel", true);
        }

        public ReactiveCommand<Unit, Unit> NavWalletCommand;
    }
}
