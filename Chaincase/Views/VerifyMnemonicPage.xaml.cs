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

                this.Bind(ViewModel, x => x.Recall0, x => x.Recall0.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel, x => x.Recall3, x => x.Recall1.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel, x => x.Recall3, x => x.Recall2.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel, x => x.Recall3, x => x.Recall3.Text)
                    .DisposeWith(disposables);

                this.Bind(ViewModel, x => x.IsVerified, x => x.IsVerifiedTriggerTrue)
                    .DisposeWith(disposables);
                this.Bind(ViewModel, x => x.IsVerified, x => x.IsVerifiedTriggerFalse)
                    .DisposeWith(disposables);
            });
		}
	}
}
