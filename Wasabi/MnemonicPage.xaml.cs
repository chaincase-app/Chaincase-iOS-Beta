using NBitcoin;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Wasabi
{
	public partial class MnemonicPage : ContentPage
	{



		public MnemonicPage()
		{
			InitializeComponent();
		}

		async void OnSubmitButtonClickedAsync(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}
	}
}
