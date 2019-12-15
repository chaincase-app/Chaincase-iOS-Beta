using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using Xamarin.Forms;
using System.Reactive.Linq;
using System.Reactive;
using System;

namespace Chaincase.Views
{
	public partial class MainPage : ReactiveContentPage<MainViewModel>
	{

        public MainPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.Balance,
                    v => v.Balance.Text)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.CoinList,
                    v => v.CoinList.ViewModel)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.NavSendCommand,
                    v => v.NavSendCommand)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.NavReceiveCommand,
                    v => v.NavReceiveCommand)
                    .DisposeWith(d);
                this.Bind(ViewModel,
                    vm => vm.Deq,
                    v => v.Deq)
                    .DisposeWith(d);
            });
        }

        private void ShowCoinJoinStatus(object sender, EventArgs args)
        {
            GoPrivateButton.Text = "Go";
            GoPrivateButton.Clicked -= ShowCoinJoinStatus;

            var unitLabel = new Label { Text = "CoinJoin unit: 0.01 BTC" };
            var coinJoinSizeLabel = new Label { Text = "CoinJoin size: 50+ persons" };
            var feeLabel = new Label { Text = "Coordination Fee: 0.5%" };
            Stackk.Children.Add(unitLabel);
            Stackk.Children.Add(coinJoinSizeLabel);
            Stackk.Children.Add(feeLabel);

            this.BindCommand(ViewModel,
                vm => vm.CoinJoin,
                v => v.GoPrivateButton,
                nameof(GoPrivateButton.Clicked));
        }
    }
}
