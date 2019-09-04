using System.Threading.Tasks;
using ReactiveUI;
using Wasabi.Navigation;

namespace Wasabi.ViewModels
{
	public class ViewModelBase : ReactiveObject, IViewModelLifecycle, IRoutableViewModel
	{
		protected readonly IScreen _hostScreen;
		public IScreen HostScreen => _hostScreen;

		public string UrlPathSegment => this.GetType().BaseType.Name.Replace("ViewModel", "");

		public ViewModelBase(IScreen hostScreen)
		{
			_hostScreen = hostScreen;
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
