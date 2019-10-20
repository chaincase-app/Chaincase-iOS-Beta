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
					vm => vm.NavSendCommand,
					v => v.NavSendCommand)
					.DisposeWith(disposables);
				this.BindCommand(ViewModel,
					vm => vm.NavReceiveCommand,
					v => v.NavReceiveCommand)
					.DisposeWith(disposables);
			});
		}
	}
}
