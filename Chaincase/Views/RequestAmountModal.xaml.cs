using System;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using System.Reactive.Disposables;
using Xamarin.Forms;
using Xamarin.Essentials;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.PlatformConfiguration;
using Splat;
using Chaincase.Navigation;

namespace Chaincase.Views
{
	public partial class RequestAmountModal : ReactiveContentPage<RequestAmountViewModel>
	{
		public RequestAmountModal()
		{
			On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FormSheet);
			InitializeComponent();

			this.WhenActivated(d =>
			{
				this.Bind(ViewModel,
					vm => vm.RequestAmount,
					v => v.RequestAmountEntry.Text)
					.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.CreateCommand,
					v => v.CreateButton)
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
