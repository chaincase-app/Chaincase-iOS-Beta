using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;
using Xamarin.Forms;
using ZXing;

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
                this.OneWayBind(ViewModel,
                    vm => vm.IsBusy,
                    v => v.IsBusy.IsRunning)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.PromptCommand,
                    v => v.Send)
                    .DisposeWith(d);
            });
        }

        protected override bool OnBackButtonPressed()
        {
            // Android only; true -> do nothing
            return ViewModel.IsBusy;
        }

        public void Handle_OnScanResult(Result result)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Scanned result", result.Text, "OK");
            });
        }
    }
}
