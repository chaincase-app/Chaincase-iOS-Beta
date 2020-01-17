using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System;

namespace Chaincase.Views
{
    public partial class LandingPage : ReactiveContentPage<LandingViewModel>
    {
        public LandingPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.BindCommand(ViewModel,
                    vm => vm.NewWalletCommand,
                    v => v.NewWalletButton)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.RecoverWalletCommand,
                    v => v.RecoverWalletButton)
                    .DisposeWith(d);
            });
        }
    }
}
