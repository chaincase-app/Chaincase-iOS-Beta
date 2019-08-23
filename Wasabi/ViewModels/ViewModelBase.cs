using System.Threading.Tasks;
using ReactiveUI;
using Wasabi.Navigation;

namespace Wasabi.ViewModels
{
	public class ViewModelBase : ReactiveObject, IViewModelLifecycle
	{
		protected readonly INavigationService _navigationService;

		public ViewModelBase(INavigationService navigationService)
		{
			_navigationService = navigationService;
		}

		public virtual Task BeforeFirstShown()
		{
			return Task.CompletedTask;
		}

		public virtual Task AfterDismissed()
		{
			return Task.CompletedTask;
		}
	}
}
