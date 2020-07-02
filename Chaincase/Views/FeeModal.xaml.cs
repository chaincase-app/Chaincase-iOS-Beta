using System;
using System.Linq;
using System.Reactive.Disposables;
using Chaincase.Converters;
using Chaincase.Navigation;
using Chaincase.ViewModels;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.XamForms;
using Splat;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Chaincase.Views
{
    public partial class FeeModal : ReactiveContentPage<FeeViewModel>
    {
        public FeeModal()
        {
            On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FormSheet);
            InitializeComponent();

			this.WhenActivated(d =>
			{
				// invert slider direction with -1
				this.OneWayBind(ViewModel,
					vm => vm.SendAmountViewModel.MaximumFeeTarget,
					v => v.FeeSlider.Minimum,
					target => -target)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.SendAmountViewModel.MinimumFeeTarget,
					v => v.FeeSlider.Maximum,
					target => -target)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.SendAmountViewModel.FeeTarget,
					v => v.FeeSlider.Value,
					target => (double)(-1 * target),
					target => (int)(-1 * target))
					.DisposeWith(d);

				this.OneWayBind(ViewModel,
					vm => vm.SendAmountViewModel.FeeRate,
					v => v.FeeLabel.Text,
					AddFeeUnits)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.SendAmountViewModel.FeeTarget,
					v => v.FeeTargetTimeLabel.Text,
					vmToViewConverterOverride: new FeeTargetTimeConverter())
				.DisposeWith(d);
			});
		}

		private string AddFeeUnits(FeeRate rate)
		{
			return "~" + rate.SatoshiPerByte.ToString() + " sat/vByte";
		}

		protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            Locator.Current.GetService<IViewStackService>().PopModal();
        }
    }
}
