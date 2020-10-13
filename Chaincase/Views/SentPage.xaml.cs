using System;
using System.Reactive.Disposables;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using Xamarin.Forms;

namespace Chaincase.Views
{
    public partial class SentPage : ReactiveContentPage<SentViewModel>
    {
        public SentPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.BindCommand(ViewModel,
                    vm => vm.NavWalletCommand,
                    v => v.DoneButton)
                    .DisposeWith(d);
            });
        }
    }
}


