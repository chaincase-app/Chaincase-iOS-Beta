using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using Xamarin.Forms;

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

		public void ShowPass(object sender, EventArgs args)
		{
			Password.IsPassword = Password.IsPassword ? false : true;
            if (ShowLabel.Text == "SHOW") {
				ShowLabel.Text = "HIDE";
            }
            else
            {
				ShowLabel.Text = "SHOW";
            }
                
		}

	}
}
