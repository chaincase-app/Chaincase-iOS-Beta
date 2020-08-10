using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System;
using Xamarin.Forms;
using Chaincase.Converters;
using NBitcoin;
using System.Globalization;
using System.Threading.Tasks;

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

				this.OneWayBind(ViewModel,
					vm => vm.CoinList.WarnCertainLink,
					v => v.NextButton.Text,
					isWarning => isWarning ? "⚠️ SEND" : "SEND")
					.DisposeWith(d);
				NextButton.Clicked += GoNext;
			}); 
		}

        private string AddFeeUnits(FeeRate rate)
        {
            return "~"+ rate.SatoshiPerByte.ToString() + " sat/vByte";
        }

		async void GoNext(object sender, EventArgs e)
		{
			Button button = (Button)sender;
			if (button.IsEnabled)
			{
				if (ViewModel.CoinList.WarnCertainLink)
				{
					bool willProceed = await WarnAndAskOkToProceed(button, e);
					if (!willProceed) return;
				}
				ViewModel.GoNext.Execute();
			}
		}

		async Task<bool> WarnAndAskOkToProceed(object sender, EventArgs e)
		{
			string warning = ViewModel.CoinList.SelectedCount == 1 ?
				"Sending the selected coin makes a sure link to you in public.\n CoinJoin first to make it private.\n Proceed anyway?" :
				"Sending the selected coins makes a sure link to you in public.\n CoinJoin first to make coins private.\n Proceed anyway?";
			bool answer = await DisplayAlert("⚠️ Warning", warning, "Yes", "No");
			return answer;
		}
	}
}
