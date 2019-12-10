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
					vm => vm.Recall[0],
					v => v.Recall0.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm .Recall[1],
					v => v.Recall1.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm.Recall[2],
					v => v.Recall2.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm.Recall[3],
					v => v.Recall3.Text)
                    .DisposeWith(disposables);
				this.Bind(ViewModel,
					vm => vm.Passphrase,
					v => v.Passphrase.Text)
				.DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm.IsVerified,
					v => v.IsVerifiedTriggerTrue)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => !vm.IsVerified,
					v => v.IsVerifiedTriggerFalse)
                    .DisposeWith(disposables);
            });
		}
	}
}
