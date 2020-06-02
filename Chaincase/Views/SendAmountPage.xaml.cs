using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System;
using Xamarin.Forms;
using Chaincase.Converters;
using NBitcoin;
using System.Globalization;

namespace Chaincase.Views
{
	public partial class SendAmountPage : ReactiveContentPage<SendAmountViewModel>
	{
		public SendAmountPage()
		{
			InitializeComponent();
			
			this.WhenActivated(d =>
			{
                this.BindCommand(ViewModel,
                    vm => vm.SelectCoins,
                    v => v.SendFromButton)
                    .DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.AmountText,
					v => v.Amount.Text)
					.DisposeWith(d);
                this.Bind(ViewModel,
                    vm => vm.IsMax,
                    v => v.Max.IsToggled)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.GoNext,
                    v => v.NextButton)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.EstimatedBtcFee,
                    v => v.FeeLabel.Text,
                    AddBalanceSymbol)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.FeeTarget,
                    v => v.FeeTargetTimeLabel.Text,
                    vmToViewConverterOverride: new FeeTargetTimeConverter())
                    .DisposeWith(d);
				SetFee(Standard, null);
            }); 
		}

        void ResetButtonBorders()
        {

            Economy.BorderWidth = 1;
            Standard.BorderWidth = 1;
            Priority.BorderWidth = 1;
        }

        private string AddBalanceSymbol(Money bal)
        {
            return "₿ " + bal.ToString();
        }

        void SetFee(object sender, EventArgs e)
        {
            switch (((Button)sender).Text)
            {
                case "Economy":
                    ViewModel.FeeChoice = Feenum.Economy;
                    break;
                case "Priority":
                    ViewModel.FeeChoice = Feenum.Priority;
                    break;
                case "Standard":
                default:
                    ViewModel.FeeChoice = Feenum.Standard;
                    break;
            }
            ResetButtonBorders();
            ((Button)sender).BorderWidth = 2;
        }
    }
}
