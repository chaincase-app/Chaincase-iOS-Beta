using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System;
using Xamarin.Forms;

namespace Chaincase.Views
{
    public partial class SendWhoPage : ReactiveContentPage<SendWhoViewModel>
    {
        public SendWhoPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.Bind(ViewModel,
                    vm => vm.Address,
                    v => v.Address.Text)
                    .DisposeWith(d);
                this.Bind(ViewModel,
                    vm => vm.Memo,
                    v => v.Memo.Text)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.PromptCommand,
                    v => v.Send)
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

        void SetFee(object sender, EventArgs e)
        {
            switch(((Button)sender).Text)
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
