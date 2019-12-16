using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;

namespace Chaincase.Views
{
	public partial class SendAmountPage : ReactiveContentPage<SendAmountViewModel>
	{

        private bool PasswordEntryIsVisible;

		public SendAmountPage()
		{
			InitializeComponent();
			
			this.WhenActivated(d =>
			{
				this.Bind(ViewModel,
					vm => vm.AmountText,
					v => v.Amount.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.CoinList,
					v => v.CoinList.ViewModel)
					.DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.GoNext,
                    v => v.NextButton)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.NavigateBack,
                    v => v.BackButton)
                    .DisposeWith(d);
			}); 
		}
    }
}
