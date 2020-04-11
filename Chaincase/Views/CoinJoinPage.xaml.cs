using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using System;

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
                this.BindCommand(ViewModel,
                    vm => vm.PromptCommand,
                    v => v.ConfirmButton)
                    .DisposeWith(d);
            });
        }
    }
}
