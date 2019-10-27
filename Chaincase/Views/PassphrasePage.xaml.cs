using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Chaincase.Views
{
	public partial class PassphrasePage : ReactiveContentPage<PassphraseViewModel>
	{

		public PassphrasePage()
		{
			InitializeComponent();
			this.WhenActivated(disposables =>
			{
				this.BindCommand(ViewModel,
					vm => vm.SubmitCommand,
					v => v.Submit)
					.DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm.Passphrase,
					v => v.Passphrase.Text)
                .DisposeWith(disposables);
            });
		}
	}
}
