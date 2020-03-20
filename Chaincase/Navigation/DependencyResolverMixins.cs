using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using ReactiveUI;
using Splat;

namespace Chaincase.Navigation
{
	public static partial class DependencyResolverMixins
	{

        /// <summary>
        /// Gets the navigation view key.
        /// </summary>
        [SuppressMessage("Design", "CA1721: Confusing name, should be method.", Justification = "Deliberate usage.")]
        public static string NavigationView => nameof(NavigationView);

        /// <summary>
        /// Initializes the sextant.
        /// </summary>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        /// <returns>The dependencyResolver.</returns>
        public static IMutableDependencyResolver RegisterNavigationView(this IMutableDependencyResolver dependencyResolver)
        {
            var vLocator = Locator.Current.GetService<IViewLocator>();

            dependencyResolver.RegisterLazySingleton(() => new NavigationView(RxApp.MainThreadScheduler, RxApp.TaskpoolScheduler, vLocator), typeof(IView), NavigationView);
            return dependencyResolver;
        }

        /// <summary>
        /// Initializes sextant.
        /// </summary>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        /// <param name="mainThreadScheduler">The main scheduler.</param>
        /// <param name="backgroundScheduler">The background scheduler.</param>
        /// <returns>The dependencyResolver.</returns>
        public static IMutableDependencyResolver RegisterNavigationView(this IMutableDependencyResolver dependencyResolver, IScheduler mainThreadScheduler, IScheduler backgroundScheduler)
        {
            var vLocator = Locator.Current.GetService<IViewLocator>();

            dependencyResolver.RegisterLazySingleton(() => new NavigationView(mainThreadScheduler, backgroundScheduler, vLocator), typeof(IView), NavigationView);
            return dependencyResolver;
        }

        /// <summary>
        /// Registers a value for navigation.
        /// </summary>
        /// <typeparam name="TView">The type of view to register.</typeparam>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        /// <param name="navigationViewFactory">The navigation view factory.</param>
        /// <returns>The dependencyResolver.</returns>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Long term object.")]
        public static IMutableDependencyResolver RegisterNavigationView<TView>(this IMutableDependencyResolver dependencyResolver, Func<TView> navigationViewFactory)
            where TView : IView
        {
            if (dependencyResolver is null)
            {
                throw new ArgumentNullException(nameof(dependencyResolver));
            }

            if (navigationViewFactory is null)
            {
                throw new ArgumentNullException(nameof(navigationViewFactory));
            }

            var navigationView = navigationViewFactory();
            var viewStackService = new ViewStackService(navigationView);

            dependencyResolver.RegisterLazySingleton<IViewStackService>(() => viewStackService);
            dependencyResolver.RegisterLazySingleton<IView>(() => navigationView, NavigationView);
            return dependencyResolver;
        }

        /// <summary>
        /// Gets the navigation view.
        /// </summary>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        /// <param name="contract">The contract.</param>
        /// <returns>The navigation view.</returns>
        public static NavigationView? GetNavigationView(
            this IReadonlyDependencyResolver dependencyResolver,
            string? contract = null)
        {
            if (dependencyResolver is null)
            {
                throw new ArgumentNullException(nameof(dependencyResolver));
            }

            return dependencyResolver.GetService<IView>(contract ?? NavigationView) as NavigationView;
        }

        /// <summary>
        /// Registers the view stack service.
        /// </summary>
        /// <typeparam name="T">The view stack service type.</typeparam>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        /// <param name="factory">The factory.</param>
        /// <returns>The dependencyResolver.</returns>
        public static IMutableDependencyResolver RegisterViewStackService<T>(this IMutableDependencyResolver dependencyResolver, Func<IView, T> factory)
            where T : IViewStackService
        {
            if (dependencyResolver is null)
            {
                throw new ArgumentNullException(nameof(dependencyResolver));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            IView view = Locator.Current.GetService<IView>(NavigationView);
            dependencyResolver.RegisterLazySingleton(() => factory(view));
            return dependencyResolver;
        }

        /// <summary>
        /// Registers the specified view with the Splat locator.
        /// </summary>
        /// <typeparam name="TView">The type of the view.</typeparam>
        /// <typeparam name="TViewModel">The type of the view model.</typeparam>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        /// <param name="contract">The contract.</param>
        /// <returns>The dependencyResovler.</returns>
        public static IMutableDependencyResolver RegisterView<TView, TViewModel>(this IMutableDependencyResolver dependencyResolver, string? contract = null)
            where TView : IViewFor<TViewModel>, new()
            where TViewModel : class, IViewModel
        {
            if (dependencyResolver is null)
            {
                throw new ArgumentNullException(nameof(dependencyResolver));
            }

            dependencyResolver.Register(() => new TView(), typeof(IViewFor<TViewModel>), contract);
            return dependencyResolver;
        }

        /// <summary>
        /// Registers the specified view with the Splat locator.
        /// </summary>
        /// <typeparam name="TView">The type of the view.</typeparam>
        /// <typeparam name="TViewModel">The type of the view model.</typeparam>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        /// <param name="viewFactory">The view factory.</param>
        /// <param name="contract">The contract.</param>
        /// <returns>The dependencyResolver.</returns>
        public static IMutableDependencyResolver RegisterView<TView, TViewModel>(this IMutableDependencyResolver dependencyResolver, Func<IViewFor<TViewModel>> viewFactory, string? contract = null)
            where TView : IViewFor
            where TViewModel : class, IViewModel
        {
            if (dependencyResolver is null)
            {
                throw new ArgumentNullException(nameof(dependencyResolver));
            }

            if (viewFactory is null)
            {
                throw new ArgumentNullException(nameof(viewFactory));
            }

            dependencyResolver.Register(viewFactory, typeof(IViewFor<TViewModel>), contract);
            return dependencyResolver;
        }

        /*
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

		private void RegisterViews(IMutableDependencyResolver dependencyResolver)
		{

			dependencyResolver.RegisterConstant(this, typeof(IViewStackService));

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

        }
        */
    }
}
