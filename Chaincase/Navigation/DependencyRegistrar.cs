using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.XamForms;
using Splat;
using Chaincase.ViewModels;
using Chaincase.Views;
using Xamarin.Forms;

namespace Chaincase.Navigation
{
	public class DependencyRegistrar
	{
        public void Register(IMutableDependencyResolver dependencyResolver, CompositionRoot compositionRoot)
		{
			dependencyResolver.RegisterLazySingleton(() => new ViewLocator(), typeof(IViewLocator));
			RegisterViews(dependencyResolver);
			RegisterScreen(dependencyResolver, compositionRoot);
		}

		private void RegisterScreen(IMutableDependencyResolver dependencyResolver, CompositionRoot compositionRoot)
		{
			dependencyResolver.RegisterLazySingleton(compositionRoot.ResolveMainView, typeof(IView));
		}

		protected T CreateView<T>()
	    where T : new()
		{
			return new T();
		}

		private void RegisterViews(IMutableDependencyResolver dependencyResolver)
		{
			dependencyResolver.Register(CreateView<MainPage>, typeof(IViewFor<MainViewModel>));
            /*
			dependencyResolver.RegisterConstant(this, typeof(IScreen));

			dependencyResolver.Register(() => new MainPage(), typeof(IViewFor<MainViewModel>));
			dependencyResolver.Register(() => new LandingPage(), typeof(IViewFor<LandingViewModel>));
			dependencyResolver.Register(() => new ReceivePage(), typeof(IViewFor<ReceiveViewModel>));
			dependencyResolver.Register(() => new AddressPage(), typeof(IViewFor<AddressViewModel>));
			dependencyResolver.Register(() => new SendAmountPage(), typeof(IViewFor<SendAmountViewModel>));
            dependencyResolver.Register(() => new SendWhoPage(), typeof(IViewFor<SendWhoViewModel>));
            dependencyResolver.Register(() => new CoinJoinPage(), typeof(IViewFor<CoinJoinViewModel>));
            dependencyResolver.Register(() => new PassphrasePage(), typeof(IViewFor<PassphraseViewModel>));
			dependencyResolver.Register(() => new MnemonicPage(), typeof(IViewFor<MnemonicViewModel>));
			dependencyResolver.Register(() => new VerifyMnemonicPage(), typeof(IViewFor<VerifyMnemonicViewModel>));
            */
		}
	}
}
