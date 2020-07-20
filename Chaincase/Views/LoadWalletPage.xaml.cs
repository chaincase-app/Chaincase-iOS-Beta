using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using Xamarin.Forms;

namespace Chaincase.Views
{
	public partial class LoadWalletPage : ReactiveContentPage<LoadWalletViewModel>
	{

		public LoadWalletPage()
		{
			InitializeComponent();
			this.WhenActivated(d =>
			{
				this.BindCommand(ViewModel,
					vm => vm.LoadWalletCommand,
					v => v.LoadWalletButton)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.Password,
					v => v.Password.Text)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.SeedWords,
					v => v.SeedWords.Text)
					.DisposeWith(d);

				ViewModel.LoadWalletCommand.Subscribe(validWords => {
					if (!validWords) Shake(SeedWords);
				});
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

		async void Shake(Entry entry)
		{
			uint timeout = 50;
			await entry.TranslateTo(-15, 0, timeout);
			await entry.TranslateTo(15, 0, timeout);
			await entry.TranslateTo(-10, 0, timeout);
			await entry.TranslateTo(10, 0, timeout);
			await entry.TranslateTo(-5, 0, timeout);
			await entry.TranslateTo(5, 0, timeout);
			entry.TranslationX = 0;
		}

	}
}
