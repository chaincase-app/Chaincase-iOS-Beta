using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.XamForms;
using Splat;
using Wasabi.ViewModels;
using Wasabi.Views;
using Xamarin.Forms;

namespace Wasabi.Navigation
{
	public class AppBootstrapper : ReactiveObject, IScreen
	{
		public AppBootstrapper(IMutableDependencyResolver dependencyResolver = null, RoutingState router = null)
		{
			Router = router ?? new RoutingState();

			RegisterParts(dependencyResolver ?? Locator.CurrentMutable);

			Router.Navigate.Execute(new MainViewModel(this));
		}

		public RoutingState Router { get; private set; }

		private void RegisterParts(IMutableDependencyResolver dependencyResolver)
		{
			dependencyResolver.RegisterConstant(this, typeof(IScreen));

			dependencyResolver.Register(() => new MainPage(), typeof(IViewFor<MainViewModel>));
			dependencyResolver.Register(() => new ReceivePage(), typeof(IViewFor<ReceiveViewModel>));
			dependencyResolver.Register(() => new CoinListPage(), typeof(IViewFor<CoinListViewModel>));
			dependencyResolver.Register(() => new PassphrasePage(), typeof(IViewFor<PassphraseViewModel>));
			dependencyResolver.Register(() => new MnemonicPage(), typeof(IViewFor<MnemonicPage>));
			dependencyResolver.Register(() => new VerifyMnemonicPage(), typeof(IViewFor<VerifyMnemonicPage>));
		}

		public Page CreateMainPage()
		{
			return new RoutedViewHost();
		}
	}
}
