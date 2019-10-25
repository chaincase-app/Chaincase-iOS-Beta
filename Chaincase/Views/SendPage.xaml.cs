using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;

namespace Chaincase.Views
{
	public partial class SendPage : ReactiveContentPage<SendViewModel>
	{
		public SendPage()
		{
			InitializeComponent();
			
			this.WhenActivated(disposables =>
			{
				this.Bind(ViewModel,
					vm => vm.Address,
					v => v.Address.Text)
					.DisposeWith(disposables);
				this.Bind(ViewModel,
					vm => vm.AmountText,
					v => v.Amount.Text)
					.DisposeWith(disposables);
				this.Bind(ViewModel,
					vm => vm.Memo,
					v => v.Memo.Text)
					.DisposeWith(disposables);
				this.Bind(ViewModel,
					vm => vm.Password,
					v => v.Password.Text)
					.DisposeWith(disposables);
				this.BindCommand(ViewModel,
					vm => vm.BuildTransactionCommand,
					v => v.Send)
					.DisposeWith(disposables);
				this.OneWayBind(ViewModel,
					vm => vm.CoinList,
					v => v.CoinList.ViewModel)
					.DisposeWith(disposables);
			});
			

		}
	}
}
