using System;
using System.Reactive.Disposables;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Chaincase.Views
{
    public partial class PasswordPromptModal : ReactiveContentPage<PasswordPromptViewModel>
    {
        public PasswordPromptModal()
        {
            On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FullScreen);
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.Bind(ViewModel,
                    vm => vm.Password,
                    v => v.Password.Text)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.AcceptText,
                    v => v.AcceptButton.Text)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                     vm => vm.ValidatePasswordCommand,
                     v => v.AcceptButton)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                     vm => vm.CancelCommand,
                     v => v.CancelButton)
                     .DisposeWith(d);
                ViewModel.ValidatePasswordCommand.Subscribe(validPassword => {
                    if (validPassword is null) Shake();
                });
            });

        }

        async void Shake()
        {
            uint timeout = 50;
            await Password.TranslateTo(-15, 0, timeout);
            await Password.TranslateTo(15, 0, timeout);
            await Password.TranslateTo(-10, 0, timeout);
            await Password.TranslateTo(10, 0, timeout);
            await Password.TranslateTo(-5, 0, timeout);
            await Password.TranslateTo(5, 0, timeout);
            Password.TranslationX = 0;
        }
    }
}
