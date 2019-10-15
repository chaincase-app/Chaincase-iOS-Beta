using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;

namespace Chaincase.Views
{
	public partial class MainPage : ReactiveContentPage<MainViewModel>
	{
		public MainPage()
		{
			InitializeComponent();

			this.WhenActivated(disposables =>
			{
				this.OneWayBind(ViewModel,
					vm => vm.Balance,
					v => v.Balance.Text)
					.DisposeWith(disposables);
				this.BindCommand(ViewModel,
					x => x.NavSendCommand,
					x => x.NavSendCommand)
					.DisposeWith(disposables);
				this.BindCommand(ViewModel,
					x => x.NavReceiveCommand,
					x => x.NavReceiveCommand)
					.DisposeWith(disposables);
			});
		}
	}
}
