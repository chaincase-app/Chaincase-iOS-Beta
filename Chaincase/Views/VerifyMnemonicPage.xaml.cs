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
					v => v.Continue)
					.DisposeWith(disposables);
				this.Bind(ViewModel,
					vm => vm.Recall0,
					v => v.Recall0.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm .Recall1,
					v => v.Recall1.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm.Recall2,
					v => v.Recall2.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm.Recall3,
					v => v.Recall3.Text)
                    .DisposeWith(disposables);
				this.Bind(ViewModel,
					vm => vm.Passphrase,
					v => v.Passphrase.Text)
				.DisposeWith(disposables);
            });
		}
	}
}
