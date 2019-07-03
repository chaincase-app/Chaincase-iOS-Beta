using System.IO;
using System.Threading.Tasks;
using WalletWasabi.Logging;
using Wasabi.Navigation;
using Wasabi.Views;
using Xamarin.Forms;

namespace Wasabi
{
	public partial class App : Application
	{
		public static INavigationService NavigationService { get; } = new NavigationService();

		public App()
		{
			InitializeComponent();
			MainPage = new NavigationPage(new PassphrasePage());

			NavigationService.Configure("MainPage", typeof(Views.MainPage));
			NavigationService.Configure("MnemonicPage", typeof(Views.MnemonicPage));
			NavigationService.Configure("VerifyMnemonicPage", typeof(Views.VerifyMnemonicPage));
			NavigationService.Configure("ReceivePage", typeof(Views.ReceivePage));
			var mainPage = ((NavigationService)NavigationService).SetRootPage("MainPage");

			MainPage = mainPage;
		}

		protected override void OnStart()
		{
			Logger.InitializeDefaults(Path.Combine(Global.DataDir, "Logs.txt"));
			Task.Run(async () => await Global.InitializeNoWalletAsync());
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
