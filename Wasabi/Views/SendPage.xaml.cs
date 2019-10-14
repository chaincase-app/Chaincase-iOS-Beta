using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Wasabi.ViewModels;

namespace Wasabi.Views
{
	public partial class SendPage : ReactiveContentPage<SendViewModel>
	{
		public SendPage()
		{
			InitializeComponent();
			
			this.WhenActivated(disposables =>
			{
				this.OneWayBind(ViewModel,
					vm => vm.CoinListViewModel,
					v => v.CoinList.ViewModel)
					.DisposeWith(disposables);
			});
			

		}
	}
}
