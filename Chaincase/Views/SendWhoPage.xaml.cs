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
            SetButtonBorders();

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
            });
        }

        void SetButtonBorders()
        {

            SlowButton.BorderWidth = 1;
            StandardButton.BorderWidth = 1;
            FastButton.BorderWidth = 1;
        }

        async void Send(object sender, EventArgs e)
        {
            string password = await DisplayPromptAsync("Confirm Send", "Enter your password.", "Confirm", "Cancel", null, -1, null, "");
            ViewModel.BuildTransactionCommand.Execute(password);
        }

        void SetFee(object sender, EventArgs e)
        {
            SetButtonBorders();
            ((Button)sender).BorderWidth = 2;
        }
    }
}
