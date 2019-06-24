using Wasabi.ViewModels;
using Xamarin.Forms;

namespace Wasabi.Views
{
	public partial class MnemonicPage : ContentPage
	{

		public MnemonicPage(string mnemonicString)
		{
			InitializeComponent();
			BindingContext = new MnemonicViewModel(App.NavigationService, mnemonicString);
		}
	}
}
