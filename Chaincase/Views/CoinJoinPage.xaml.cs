using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System;
using Chaincase.Converters;
using Chaincase.Models;
using NBitcoin;

namespace Chaincase.Views
{
    public partial class CoinJoinPage : ReactiveContentPage<CoinJoinViewModel>
	{
        private TimeSpanStringConverter _timeSpanStringConverter;
        private PhaseStringConverter _phaseStringConverter;

        public CoinJoinPage()
        {
            InitializeComponent();

            _timeSpanStringConverter = new TimeSpanStringConverter();
            _phaseStringConverter = new PhaseStringConverter();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.CoinList,
                    v => v.CoinList.ViewModel)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.AmountQueued,
                    v => v.WarnToOpenLabel.IsVisible,
                    amount => amount.CompareTo(Money.Zero) != 0)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.RoundPhaseState,
                    v => v.PhaseLabel.Text,
                    FormatPhaseLabel)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.TimeLeftTillRoundTimeout,
                    v => v.TimeLeftLabel.Text,
                    FormatTimeLeftLabel)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.PromptCommand,
                    v => v.CoinJoinButton)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.AmountQueued,
                    v => v.CoinJoinButton.IsVisible,
                    amount => !(amount.CompareTo(Money.Zero) != 0))
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.ExitCoinJoinCommand,
                    v => v.ExitButton)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.AmountQueued,
                    v => v.ExitButton.IsVisible,
                    amount => amount.CompareTo(Money.Zero) != 0)
                    .DisposeWith(d);
            });
        }

        private bool AmountQueuedNonzero(Money amount)
		{
            return amount.CompareTo(Money.Zero) != 0;
		}

        private string FormatTimeLeftLabel(TimeSpan t)
        {
            _timeSpanStringConverter.TryConvert(t, typeof(string), null, out var timeString);
            return "Registration ends in " + timeString;
        }

        private string FormatPhaseLabel(RoundPhaseState rps)
        {
            _phaseStringConverter.TryConvert(rps, typeof(string), null, out var phaseString);
            return "Phase: " + phaseString;
        }
    }
}
