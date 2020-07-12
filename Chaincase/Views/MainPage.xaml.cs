using System.Reactive.Disposables;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using Xamarin.Forms;
using System.Reactive.Linq;
using System.Reactive;
using System;
using System.Linq;

namespace Chaincase.Views
{
    public partial class MainPage : ReactiveContentPage<MainViewModel>
	{

        public MainPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.BindCommand(ViewModel,
                    vm => vm.ShowWalletInfoCommand,
                    v => v.WalletInfoButton)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.Balance,
                    v => v.Balance.Text,
                    AddBalanceSymbol)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.StatusViewModel.Status, 
                    v => v.Balance.TextColor,
                    ConvertStatusToColor)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.Transactions,
                    v => v.Transactions.ItemsSource)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel,
                    vm => vm.Transactions,
                    v => v.Transactions.IsVisible,
                    txs => txs.Count() > 0)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.Transactions,
                    v => v.NewWalletLabel.IsVisible,
                    txs => txs.Count() == 0)
                    .DisposeWith(d);
                this.Bind(ViewModel,
                    vm => vm.StatusViewModel,
                    v => v.Status.ViewModel);

                // performance enhansement: only create button on !backedUp
                // (no binding)
                this.BindCommand(ViewModel,
                    vm => vm.NavBackUpCommand,
                    v => v.BackUp)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.CanBackUp,
                    v => v.BackUp.IsVisible,
                    canBackUp => canBackUp)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                    vm => vm.NavReceiveCommand,
                    v => v.NavReceiveCommand)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.InitCoinJoin,
                    v => v.CoinJoinButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                    vm => vm.SendCommand,
                    v => v.SendButton)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.HasCoins,
                    v => v.SendButton.IsVisible)
                    .DisposeWith(d);
            });

            Transactions.ItemTapped += (_, e) =>
            {
                TransactionViewModel tvm = (TransactionViewModel)e.Item;
                ViewModel.OpenTransactionDetail.Execute(tvm).Subscribe();
            }; ;
        }

        private string AddBalanceSymbol(string bal)
        {
            return "₿ " + bal;
        }

        private Color ConvertStatusToColor(string status)
        {
            if (status == "Ready")
            {
                return Color.Default;
            }
            return Color.LightGray;
        }
    }
}
