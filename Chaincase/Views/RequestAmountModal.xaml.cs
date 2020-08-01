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
using System.Text.RegularExpressions;
using System.Linq;

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


				RequestAmountEntry.TextChanged += Entry_TextChanged;
			});
		}

		void Entry_TextChanged(object sender, TextChangedEventArgs e)
		{
			var oldText = e.OldTextValue;
			var newText = e.NewTextValue;

			// Correct amount
			Regex digitsOnly = new Regex(@"[^\d,.]");
			string betterAmount = digitsOnly.Replace(e.NewTextValue, ""); // Make it digits , and . only.

			betterAmount = betterAmount.Replace(',', '.');
			int countBetterAmount = betterAmount.Count(x => x == '.');
			if (countBetterAmount > 1) // Do not enable typing two dots.
			{
				var index = betterAmount.IndexOf('.', betterAmount.IndexOf('.') + 1);
				if (index > 0)
				{
					betterAmount = betterAmount.Substring(0, index);
				}
			}
			var dotIndex = betterAmount.IndexOf('.');
			if (dotIndex != -1 && betterAmount.Length - dotIndex > 8) // Enable max 8 decimals.
			{
				betterAmount = betterAmount.Substring(0, dotIndex + 1 + 8);
			}

			if (betterAmount != e.NewTextValue)
			{
				((Xamarin.Forms.Entry)sender).Text = betterAmount;
			}
		}

		protected override async void OnDisappearing()
		{
			base.OnDisappearing();
			Locator.Current.GetService<IViewStackService>().PopModal();
		}
	}
}
