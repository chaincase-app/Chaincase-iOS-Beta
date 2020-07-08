using System;
using System.Linq;
using System.Reactive.Disposables;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Chaincase.Views
{
    public partial class BackUpModal : ReactiveContentPage<BackUpViewModel>
    {
        public BackUpModal()
        {
            On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FullScreen);
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.BindCommand(ViewModel,
                    vm => vm.VerifyCommand,
                    v => v.VerifyButton)
                    .DisposeWith(d);

                VerifyButton.Clicked += delegate
                {
                    Carousel.Position = 0;
                };
            });

        }

        public void OnBack(object subject, EventArgs e)
        {
            Carousel.Position--;
        }

        public void OnNext(object subject, EventArgs e)
        {
            Carousel.Position++;
        }


        public void OnPositionChanged(object subject, EventArgs e)
        {
            var pos = Carousel.Position;
            if (pos == 0)
            {
                Grid.SetColumn(NextButton, 0);
                Grid.SetColumnSpan(NextButton, 2);
                BackButton.IsVisible = false;
                VerifyButton.IsVisible = false;
                NextButton.IsVisible = true;
            }
            else if (pos < ViewModel.SeedWords.Count() - 1)
            {
                BackButton.IsVisible = true;
                NextButton.IsVisible = true;
                VerifyButton.IsVisible = false;
                Grid.SetColumn(NextButton, 1);
                Grid.SetColumnSpan(NextButton, 1);
            }
            else
            {
                BackButton.IsVisible = true;
                NextButton.IsVisible = false;
                VerifyButton.IsVisible = true;              
            }
        }
    }
}
