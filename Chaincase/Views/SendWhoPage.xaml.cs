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
                    vm => vm.Memo,
                    v => v.Memo.Text)
                    .DisposeWith(d);
            });
        }
    }
}
