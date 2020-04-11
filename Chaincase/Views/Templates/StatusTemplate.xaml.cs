using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Chaincase.Views.Templates
{
    public partial class StatusTemplate : ReactiveContentView<StatusViewModel>
    {
        public StatusTemplate()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.Status,
                    v => v.StatusLabel.Text)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.Status,
                    v => v.Indicator.IsRunning,
                    IsStatusNotReadyConv)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.Status,
                    v => v.Indicator.IsVisible,
                    IsStatusNotReadyConv)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.Status,
                    v => v.ReadyIcon.IsVisible,
                    IsStatusReadyConv)
                    .DisposeWith(d);
            });
        }

        private bool IsStatusReadyConv(string status)
        {
            return status == "Ready";
        }

        private bool IsStatusNotReadyConv(string status)
        {
            return status != "Ready";
        }
}
}
