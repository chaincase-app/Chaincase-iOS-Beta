using System;
using System.Reactive;
using Chaincase.Navigation;
using ReactiveUI;
using NBitcoin;
using Splat;

namespace Chaincase.ViewModels
{
    public class RequestAmountViewModel : ViewModelBase
    {
        private string _requestAmount;

        public RequestAmountViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
        {
            RequestAmount = "0";

            CreateCommand = ReactiveCommand.CreateFromObservable<Unit, Unit>(_ =>
            {
                return ViewStackService.PopModal();
            });
        }

        public ReactiveCommand<Unit, Unit> CreateCommand;

        public string RequestAmount
        {
            get => _requestAmount;
            set => this.RaiseAndSetIfChanged(ref _requestAmount, value);
        }
    }
}
