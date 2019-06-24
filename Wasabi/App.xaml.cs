using System.IO;
using WalletWasabi.Helpers;
using Wasabi.Navigation;
using Wasabi.Views;
using Xamarin.Forms;

namespace Wasabi
{
	public partial class App : Application
	{
		public static INavigationService NavigationService { get; } = new NavigationService();

		public static string DataDir { get; private set; }
		public static string WalletsDir { get; private set;  }

		public App()
		{
			InitializeComponent();

			MainPage = new NavigationPage(new MainPage());

			NavigationService.Configure("MainPage", typeof(Views.MainPage));
			NavigationService.Configure("MnemonicPage", typeof(Views.MnemonicPage));
			NavigationService.Configure("VerifyMnemonicPage", typeof(Views.VerifyMnemonicPage));
			var mainPage = ((NavigationService)NavigationService).SetRootPage("MainPage");

			MainPage = mainPage;

			DataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Wasabi", "Client"));
			WalletsDir = Path.Combine(DataDir, "Wallets");
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
