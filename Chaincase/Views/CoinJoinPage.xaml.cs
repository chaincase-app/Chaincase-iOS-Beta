using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;

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
                    vm => vm.Balance,
                    v => v.Balance.Text)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.CoinList,
                    v => v.CoinList.ViewModel)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.CoinJoin,
                    v => v.ConfirmButton)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                    vm => vm.NavigateBack,
                    v => v.CancelButton)
                    .DisposeWith(d);
            });
        }
    }
}
