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

        private CompositeDisposable Disposables { get; set; }

        public MainPage()
        {
            InitializeComponent();

            this.WhenActivated(Disposables =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.Balance,
                    v => v.Balance.Text)
                    .DisposeWith(Disposables);
                this.BindCommand(ViewModel,
                    vm => vm.NavSendCommand,
                    v => v.NavSendCommand)
                    .DisposeWith(Disposables);
                this.BindCommand(ViewModel,
                    vm => vm.NavReceiveCommand,
                    v => v.NavReceiveCommand)
                    .DisposeWith(Disposables);
            });
        }

        private void ShowCoinJoinStatus(object sender, EventArgs args)
        {
            var unitLabel = new Label { Text = "CoinJoin unit: 0.01 BTC" };
            var coinJoinSizeLabel = new Label { Text = "CoinJoin size: 50+ persons" };
            var feeLabel = new Label { Text = "Coordination Fee: 0.5%" };
            Stackk.Children.Add(unitLabel);
            Stackk.Children.Add(coinJoinSizeLabel);
            Stackk.Children.Add(feeLabel);

            this.BindCommand(ViewModel,
                vm => vm.CoinJoinCommand,
                v => v.GoPrivateButton);
        }
    }
}
