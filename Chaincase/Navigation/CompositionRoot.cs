using ReactiveUI;
using Splat;
using System;
using System.Reactive.Concurrency;
using System.Threading;
using Chaincase.Views;
using Xamarin.Forms;

namespace Chaincase.Navigation
{
    public class CompositionRoot
    {
        private Lazy<IScheduler> BackgroundScheduler;
        private Lazy<IViewLocator> CurrentViewLocator;
        private Lazy<IScheduler> MainScheduler;
        private Lazy<MainView> MainView;
        public Lazy<App> App { get; set; }

        public Func<MainView> ResolveMainView => () => MainView.Value;

        private Func<MainView> MainViewFactory => () => new MainView(BackgroundScheduler.Value, MainScheduler.Value, CurrentViewLocator.Value, new Page());

        public CompositionRoot()
        {
            App = new Lazy<App>(CreateApp);
            MainScheduler = new Lazy<IScheduler>(CreateMainScheduler);
            BackgroundScheduler = new Lazy<IScheduler>(CreateBackgroundScheduler);
            CurrentViewLocator = new Lazy<IViewLocator>(CreateViewLocator);
            MainView = new Lazy<MainView>(MainViewFactory);
        }

        public App ResolveApp() => App.Value;

        private App CreateApp() => new App(MainViewFactory);

        private IScheduler CreateBackgroundScheduler() => new EventLoopScheduler();

        private IScheduler CreateMainScheduler() => new SynchronizationContextScheduler(SynchronizationContext.Current);

        private IViewLocator CreateViewLocator() => Locator.Current.GetService<IViewLocator>();
    }
}