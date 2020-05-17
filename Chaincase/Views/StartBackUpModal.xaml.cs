using System.Reactive.Disposables;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Chaincase.Views
{
    public partial class StartBackUpModal : ReactiveContentPage<StartBackUpViewModel>
    {
        public StartBackUpModal()
        {
            On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FullScreen);
            InitializeComponent();

            this.WhenActivated(d =>
            {

                this.BindCommand(ViewModel,
                    vm => vm.NextCommand,
                    v => v.NextButton)
                    .DisposeWith(d);
            });
        }
    }
}
