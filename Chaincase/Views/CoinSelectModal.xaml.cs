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
				this.Bind(ViewModel,
					vm => vm.SelectPrivateSwitchState,
					v => v.SelectPrivateSwitch.IsToggled)
					.DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm,
                    v => v.CoinList.ViewModel)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.CoinList,
                    v => v.CoinList.IsVisible,
                    coins => coins.Count() > 0)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel,
                    vm => vm.CoinList,
                    v => v.EmptyWalletLabel.IsVisible,
                    coins => coins.Count() == 0)
                    .DisposeWith(d);
            });
        }

        void OnSelectPrivateToggled(object sender, ToggledEventArgs e)
        {
            ViewModel.SelectPrivateSwitchCommand.Execute().Subscribe();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            Locator.Current.GetService<IViewStackService>().PopModal();
        }
    }
}
