using System;
using System.Reactive;
using Chaincase.Navigation;
using ReactiveUI;
using Splat;

namespace Chaincase.ViewModels
{
    public class FeeViewModel : ViewModelBase
    {
        private SendAmountViewModel _sendAmountViewModel;

        public ReactiveCommand<Unit, Unit> NavBackCommand;

        public FeeViewModel(SendAmountViewModel sendAmountViewModel)
            : base(Locator.Current.GetService<IViewStackService>())
        {
            SendAmountViewModel = sendAmountViewModel;

            NavBackCommand = ReactiveCommand.CreateFromObservable<Unit, Unit>(_ =>
            {
                return ViewStackService.PopModal();
            });
        }

        public SendAmountViewModel SendAmountViewModel
        {
            get => _sendAmountViewModel;
            set => this.RaiseAndSetIfChanged(ref _sendAmountViewModel, value);
        }
    }
}
