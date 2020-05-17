using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Chaincase.Views
{
	public partial class NewPasswordPage : ReactiveContentPage<NewPasswordViewModel>
	{

		public NewPasswordPage()
		{
			InitializeComponent();
			this.WhenActivated(d =>
			{
				this.BindCommand(ViewModel,
					vm => vm.SubmitCommand,
					v => v.Submit)
					.DisposeWith(d);
                this.Bind(ViewModel,
					vm => vm.Password,
					v => v.Password.Text)
                .DisposeWith(d);
            });
		}
	}
}
