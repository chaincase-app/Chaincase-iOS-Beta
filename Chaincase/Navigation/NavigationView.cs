using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ReactiveUI;
using Splat;
using WalletWasabi.Helpers;
using Xamarin.Forms;

namespace Chaincase.Navigation
{
    public sealed class NavigationView : NavigationPage, IView
    {
        private readonly IScheduler _backgroundScheduler;
        private readonly IScheduler _mainScheduler;
        private readonly IViewLocator _viewLocator;
        private readonly IObservable<IViewModel> pagePopped;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationView"/> class.
        /// </summary>
        public NavigationView()
            : this(RxApp.MainThreadScheduler, RxApp.TaskpoolScheduler, Locator.Current.GetService<IViewLocator>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationView"/> class.
        /// </summary>
        /// <param name="mainScheduler">The main scheduler to scheduler UI tasks on.</param>
        /// <param name="backgroundScheduler">The background scheduler.</param>
        /// <param name="viewLocator">The view locator which will find views associated with view models.</param>
        /// <param name="rootPage">The starting root page.</param>
        public NavigationView(IScheduler backgroundScheduler, IScheduler mainScheduler, IViewLocator viewLocator, Page rootPage)
            : base(rootPage)
        {
            _backgroundScheduler = backgroundScheduler;
            _mainScheduler = mainScheduler;
            _viewLocator = viewLocator;

            this.pagePopped = Observable
                .FromEventPattern<NavigationEventArgs>(x => this.Popped += x, x => this.Popped -= x)
                .Select(ep => ep.EventArgs.Page.BindingContext as IViewModel)
                .Where(e => e != null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationView"/> class.
        /// </summary>
        /// <param name="mainScheduler">The main scheduler to scheduler UI tasks on.</param>
        /// <param name="backgroundScheduler">The background scheduler.</param>
        /// <param name="viewLocator">The view locator which will find views associated with view models.</param>
        public NavigationView(IScheduler mainScheduler, IScheduler backgroundScheduler, IViewLocator viewLocator)
        {
            _backgroundScheduler = backgroundScheduler;
            _mainScheduler = mainScheduler;
            _viewLocator = viewLocator;

            PagePopped =
                Observable
                    .FromEventPattern<NavigationEventArgs>(x => Popped += x, x => Popped -= x)
                    .Select(ep => ep.EventArgs.Page.BindingContext as IViewModel)
                    .Where(x => x != null);
        }

        public IObservable<IViewModel?> PagePopped { get; }

        public IObservable<Unit> PushModal(IViewModel modalViewModel, string contract)
        {
            Guard.NotNull(nameof(modalViewModel), modalViewModel);

            return Observable
                .Start(
                    () =>
                    {
                        var page = this.LocatePageFor(modalViewModel, contract);
                        this.SetPageTitle(page, modalViewModel.Id);
                        return page;
                    },
                    _backgroundScheduler)
                .ObserveOn(_mainScheduler)
                .SelectMany(
                    page =>
                        this
                            .Navigation
                            .PushModalAsync(page)
                            .ToObservable());
        }

        public IObservable<Unit> PopModal() =>
            this
                .Navigation
                .PopModalAsync()
                .ToObservable()
                .Select(_ => Unit.Default)
                // XF completes the pop operation on a background thread :/
                .ObserveOn(_mainScheduler);

        public IObservable<Unit> PushPage(IViewModel pageViewModel, string contract, bool resetStack, bool animate)
        {
            Guard.NotNull(nameof(pageViewModel), pageViewModel);

            // If we don't have a root page yet, be sure we create one and assign one immediately because otherwise we'll get an exception.
            // Otherwise, create it off the main thread to improve responsiveness and perceived performance.
            var hasRoot = this.Navigation.NavigationStack.Count > 0;
            var mainScheduler = hasRoot ? _mainScheduler : CurrentThreadScheduler.Instance;
            var backgroundScheduler = hasRoot ? _backgroundScheduler : CurrentThreadScheduler.Instance;

            return Observable
                .Start(
                    () =>
                    {
                        var page = this.LocatePageFor(pageViewModel, contract);
                        this.SetPageTitle(page, pageViewModel.Id);
                        return page;
                    },
                    backgroundScheduler)
                .ObserveOn(mainScheduler)
                .SelectMany(
                    page =>
                    {
                        if (resetStack)
                        {
                            if (this.Navigation.NavigationStack.Count == 0)
                            {
                                return this
                                    .Navigation
                                    .PushAsync(page, animated: false)
                                    .ToObservable();
                            }
                            else
                            {
                                // XF does not allow us to pop to a new root page. Instead, we need to inject the new root page and then pop to it.
                                this
                                    .Navigation
                                    .InsertPageBefore(page, this.Navigation.NavigationStack[0]);

                                return this
                                    .Navigation
                                    .PopToRootAsync(animated: false)
                                    .ToObservable();
                            }
                        }
                        else
                        {
                            return this
                                .Navigation
                                .PushAsync(page, animate)
                                .ToObservable();
                        }
                    });
        }

        public IObservable<Unit> PopPage(bool animate) =>
            this
                .Navigation
                .PopAsync(animate)
                .ToObservable()
                .Select(_ => Unit.Default)
                // XF completes the pop operation on a background thread :/
                .ObserveOn(_mainScheduler);

        private Page LocatePageFor(object viewModel, string contract)
        {
            Guard.NotNull(nameof(viewModel), viewModel);

            var view = _viewLocator.ResolveView(viewModel, contract);
            var viewFor = view as IViewFor;
            var page = view as Page;

            if (view == null)
            {
                throw new InvalidOperationException($"No view could be located for type '{viewModel.GetType().FullName}', contract '{contract}'. Be sure Splat has an appropriate registration.");
            }

            if (viewFor == null)
            {
                throw new InvalidOperationException($"Resolved view '{view.GetType().FullName}' for type '{viewModel.GetType().FullName}', contract '{contract}' does not implement IViewFor.");
            }

            if (page == null)
            {
                throw new InvalidOperationException($"Resolved view '{view.GetType().FullName}' for type '{viewModel.GetType().FullName}', contract '{contract}' is not a Page.");
            }

            viewFor.ViewModel = viewModel;

            return page;
        }

        private void SetPageTitle(Page page, string resourceKey)
        {
            // var title = Localize.GetString(resourceKey);
            // TODO: ensure resourceKey isn't null and is localized.
            page.Title = resourceKey;
        }
    }
}
