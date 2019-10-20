using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Chaincase.Views
{
	public partial class VerifyMnemonicPage : ReactiveContentPage<VerifyMnemonicViewModel>
	{
		public VerifyMnemonicPage()
		{
			InitializeComponent();
			this.WhenActivated(disposables =>
			{
				this.BindCommand(ViewModel,
					vm => vm.TryCompleteInitializationCommand,
					x => x.Continue)
					.DisposeWith(disposables);
			});
		}
	}
}
