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
				this.OneWayBind(ViewModel,
					vm => vm.CoinList,
					v => v.CoinList.ViewModel)
					.DisposeWith(disposables);
			});
			

		}
	}
}
