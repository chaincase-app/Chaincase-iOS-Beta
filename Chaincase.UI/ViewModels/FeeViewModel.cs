using System;
using System.Reactive;
using ReactiveUI;
using Splat;

namespace Chaincase.UI.ViewModels
{
    public class FeeViewModel : ReactiveObject
    {
        private SendViewModel _sendViewModel;


        public FeeViewModel(SendViewModel sendViewModel)
        {
            SendViewModel = sendViewModel;
        }

        public SendViewModel SendViewModel
        {
            get => _sendViewModel;
            set => this.RaiseAndSetIfChanged(ref _sendViewModel, value);
        }
    }
}
