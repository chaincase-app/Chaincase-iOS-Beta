using System;
using Chaincase.Navigation;
using ReactiveUI;
using Splat;

namespace Chaincase.ViewModels
{
    public class FeeViewModel : ViewModelBase
    {
        private SendAmountViewModel _sendAmountViewModel;

        public FeeViewModel(SendAmountViewModel sendAmountViewModel)
            : base(Locator.Current.GetService<IViewStackService>())
        {
            SendAmountViewModel = sendAmountViewModel;
        }

        public SendAmountViewModel SendAmountViewModel
        {
            get => _sendAmountViewModel;
            set => this.RaiseAndSetIfChanged(ref _sendAmountViewModel, value);
        }
    }
}
