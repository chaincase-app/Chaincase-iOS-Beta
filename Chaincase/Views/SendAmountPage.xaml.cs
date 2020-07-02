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

				// invert slider direction with -1
				this.OneWayBind(ViewModel,
					vm => vm.MaximumFeeTarget,
					v => v.FeeSlider.Minimum,
					target => -target)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.MinimumFeeTarget,
					v => v.FeeSlider.Maximum,
					target => -target)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.FeeTarget,
					v => v.FeeSlider.Value,
					target => -1 * target,
					target => (int)(-1 * target))
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.FeeRate,
					v => v.FeeLabel.Text,
					AddFeeUnits)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.FeeTarget,
					v => v.FeeTargetTimeLabel.Text,
					vmToViewConverterOverride: new FeeTargetTimeConverter())
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
