using Wasabi.ViewModels;
using Xamarin.Forms;

namespace Wasabi.Views
{
	public partial class PassphrasePage : ContentPage
	{

		public PassphrasePage()
		{
			InitializeComponent();
			BindingContext = new PassphraseViewModel(App.NavigationService);
		}
	}
}
