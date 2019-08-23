using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Wasabi.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Wasabi.Navigation
{
	public class NavigationService : INavigationService
	{
		private readonly IHaveMainPage _presentationRoot;
		private readonly IViewLocator _viewLocator;

		public NavigationService(IHaveMainPage presentationRoot, IViewLocator viewLocator)
		{
			_presentationRoot = presentationRoot;
			_viewLocator = viewLocator;
		}

		private Xamarin.Forms.INavigation Navigator => _presentationRoot.MainPage.Navigation;

		public void PresentAsMainPage(ViewModelBase viewModel)
		{
			var page = _viewLocator.CreateAndBindPageFor(viewModel);

			IEnumerable<ViewModelBase> viewModelsToDismiss = FindViewModelsToDismiss(_presentationRoot.MainPage);

			if (_presentationRoot.MainPage is NavigationPage navPage)
			{
				// If we're replacing a navigation page, unsub from events
				navPage.PopRequested -= NavPagePopRequested;
			}

			viewModel.BeforeFirstShown();

			_presentationRoot.MainPage = page;

			foreach (ViewModelBase toDismiss in viewModelsToDismiss)
			{
				toDismiss.AfterDismissed();
			}
		}

		public void PresentAsNavigatableMainPage(ViewModelBase viewModel)
		{
			var page = _viewLocator.CreateAndBindPageFor(viewModel);

			NavigationPage newNavigationPage = new NavigationPage(page);

			IEnumerable<ViewModelBase> viewModelsToDismiss = FindViewModelsToDismiss(_presentationRoot.MainPage);

			if (_presentationRoot.MainPage is NavigationPage navPage)
			{
				navPage.PopRequested -= NavPagePopRequested;
			}

			viewModel.BeforeFirstShown();

			// Listen for back button presses on the new navigation bar
			newNavigationPage.PopRequested += NavPagePopRequested;
			_presentationRoot.MainPage = newNavigationPage;

			foreach (ViewModelBase toDismiss in viewModelsToDismiss)
			{
				toDismiss.AfterDismissed();
			}
		}

		private IEnumerable<ViewModelBase> FindViewModelsToDismiss(Page dismissingPage)
		{
			var viewmodels = new List<ViewModelBase>();

			if (dismissingPage is NavigationPage)
			{
				viewmodels.AddRange(
					Navigator
						.NavigationStack
						.Select(p => p.BindingContext)
						.OfType<ViewModelBase>()
				);
			}
			else
			{
				var viewmodel = dismissingPage?.BindingContext as ViewModelBase;
				if (viewmodel != null) viewmodels.Add(viewmodel);
			}

			return viewmodels;
		}

		private void NavPagePopRequested(object sender, NavigationRequestedEventArgs e)
		{
			if (Navigator.NavigationStack.LastOrDefault()?.BindingContext is ViewModelBase poppingPage)
			{
				poppingPage.AfterDismissed();
			}
		}

		public async Task NavigateTo(ViewModelBase viewModel)
		{
			var page = _viewLocator.CreateAndBindPageFor(viewModel);

			await viewModel.BeforeFirstShown();

			await Navigator.PushAsync(page);
		}

		public async Task NavigateBack()
		{
			var dismissing = Navigator.NavigationStack.Last().BindingContext as ViewModelBase;

			await Navigator.PopAsync();

			dismissing?.AfterDismissed();
		}

		public async Task NavigateBackToRoot()
		{
			var toDismiss = Navigator
				.NavigationStack
				.Skip(1)
				.Select(vw => vw.BindingContext)
				.OfType<ViewModelBase>()
				.ToArray();

			await Navigator.PopToRootAsync();

			foreach (ViewModelBase viewModel in toDismiss)
			{
				// TODO test this .. It was orig. viewModel.AfterDismissed().FireAndForget();
				_ = Task.Run(async () => await viewModel.AfterDismissed());
			}
		}
	}
}
