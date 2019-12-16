using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System;

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
                    vm => vm.Password,
                    v => v.Password.Text)
                    .DisposeWith(d);
                this.Bind(ViewModel,
                    vm => vm.Memo,
                    v => v.Memo.Text)
                    .DisposeWith(d);
            });
        }

        public void ShowPasswordEntry(object sender, EventArgs args)
        {
            PasswordModal.IsVisible = true;
        }

        public void HidePasswordEntry(object sender, EventArgs args)
        {
            PasswordModal.IsVisible = false;
        }
    }
}
