using System;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using System.Reactive.Disposables;
using Xamarin.Forms;

namespace Chaincase.Views
{
	public partial class VerifyMnemonicPage : ReactiveContentPage<VerifyMnemonicViewModel>
	{
		public VerifyMnemonicPage()
		{
			InitializeComponent();
			this.WhenActivated(d =>
			{
				this.Bind(ViewModel,
					vm => vm.Recall0,
					v => v.Recall0.Text)
                    .DisposeWith(d);
                this.Bind(ViewModel,
					vm => vm .Recall1,
					v => v.Recall1.Text)
                    .DisposeWith(d);
                this.Bind(ViewModel,
					vm => vm.Recall2,
					v => v.Recall2.Text)
                    .DisposeWith(d);
                this.Bind(ViewModel,
					vm => vm.Recall3,
					v => v.Recall3.Text)
                    .DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.Passphrase,
					v => v.Password.Text)
				.DisposeWith(d);
				this.BindCommand(ViewModel,
	                vm => vm.VerifyCommand,
	                v => v.VerifyButton)
	                .DisposeWith(d);
                ViewModel.VerifyCommand.Subscribe(verified =>{
					if (!verified) Shake();
				});
			});
		}

		async void Shake()
		{
			uint timeout = 50;
			await VerifyButton.TranslateTo(-15, 0, timeout);
			await VerifyButton.TranslateTo(15, 0, timeout);
			await VerifyButton.TranslateTo(-10, 0, timeout);
			await VerifyButton.TranslateTo(10, 0, timeout);
			await VerifyButton.TranslateTo(-5, 0, timeout);
			await VerifyButton.TranslateTo(5, 0, timeout);
			VerifyButton.TranslationX = 0;
		}
	}
}
