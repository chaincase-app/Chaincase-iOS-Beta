using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using WalletWasabi.Models;
using Xamarin.Forms;

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
                    vm => vm.Tor,
                    v => v.TorLabel.Text,
                    TorStatusConv);
                this.OneWayBind(ViewModel,
                    vm => vm.Status,
                    v => v.StatusLabel.Text)
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

        private string TorStatusConv(TorStatus status)
        {
            switch(status)
            {
                case TorStatus.Running:
                    return "🧅";
                default:
                    return "";
            }
        }
    }

    public static class AttachedProperties
    {
        public static BindableProperty AnimatedProgressProperty =
           BindableProperty.CreateAttached("AnimatedProgress",
                                           typeof(double),
                                           typeof(ProgressBar),
                                           0.0d,
                                           BindingMode.OneWay,
                                           propertyChanged: (b, o, n) =>
                                           ProgressBarProgressChanged((ProgressBar)b, (double)n));

        private static void ProgressBarProgressChanged(ProgressBar progressBar, double progress)
        {
            ViewExtensions.CancelAnimations(progressBar);
            var lengthMs = progress == 1 ? 100 : (uint)((progress - progressBar.Progress) * 40000);
            progressBar.ProgressTo((double)progress, lengthMs, Easing.SinIn);
        }
    }
}
