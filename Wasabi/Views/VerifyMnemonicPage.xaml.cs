using System;
using System.Collections.Generic;
using Wasabi.ViewModels;
using Xamarin.Forms;

namespace Wasabi.Views
{
	public partial class VerifyMnemonicPage : ContentPage
	{
		public VerifyMnemonicPage(string mnemonicString)
		{
			InitializeComponent();
			BindingContext = new VerifyMnemonicViewModel(App.NavigationService, mnemonicString);

		}
	}
}
