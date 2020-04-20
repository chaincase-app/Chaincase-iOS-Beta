using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System;
using Chaincase.Converters;

namespace Chaincase.Views
{
    public partial class CoinJoinPage : ReactiveContentPage<CoinJoinViewModel>
	{

        public CoinJoinPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.CoinList,
                    v => v.CoinList.ViewModel)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.RoundPhaseState,
                    v => v.PhaseLabel.Text,
                    vmToViewConverterOverride: new PhaseStringConverter())
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.TimeLeftTillRoundTimeout,
                    v => v.TimeLeftLabel.Text,
                    vmToViewConverterOverride: new TimeSpanStringConverter())
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.PromptCommand,
                    v => v.ConfirmButton)
                    .DisposeWith(d);
            });
        }
    }
}
