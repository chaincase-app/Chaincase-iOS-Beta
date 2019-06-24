using System;
using Wasabi;
using Wasabi.ViewModels;
using Xamarin.Forms;

namespace Wasabi.Views
{
	public partial class MainPage : ContentPage
	{

		public MainPage()
		{
			InitializeComponent();
			BindingContext = new MainViewModel(App.NavigationService);
		}
	}
}
