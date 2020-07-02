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
				this.OneWayBind(ViewModel,
					vm => vm.SendFromText,
					v => v.SendFromButton.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.CoinList.SelectedAmount,
					v => v.AvailableLabel.Text,
					amt => "Available: ₿ " + amt.ToString())
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.AmountText,
					v => v.Amount.Text)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.IsMax,
					v => v.MaxSwitch.IsToggled)
					.DisposeWith(d);

				this.BindCommand(ViewModel,
					vm => vm.SelectFee,
					v => v.FeeButton)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.FeeRate,
					v => v.FeeButton.Text,
					AddFeeUnits)
					.DisposeWith(d);

				this.BindCommand(ViewModel,
					vm => vm.GoNext,
					v => v.NextButton)
					.DisposeWith(d);

			}); 
		}

        private string AddFeeUnits(FeeRate rate)
        {
            return "~"+ rate.SatoshiPerByte.ToString() + " sat/vByte";
        }
    }
}
