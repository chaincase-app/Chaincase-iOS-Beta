using System;
using System.Linq;
using System.Reactive.Disposables;
using Chaincase.Navigation;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using Splat;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Chaincase.Views
{
    public partial class CoinSelectModal : ReactiveContentPage<CoinListViewModel>
    {
        public CoinSelectModal()
        {
            On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FormSheet);
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.Coins,
                    v => v.Coins.ItemsSource)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.Coins,
                    v => v.Coins.IsVisible,
                    coins => coins.Count() > 0)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.Coins,
                    v => v.EmptyWalletLabel.IsVisible,
                    coins => coins.Count() == 0)
                    .DisposeWith(d);
            });

        }
	
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            Locator.Current.GetService<IViewStackService>().PopModal();
        }
    }
}
