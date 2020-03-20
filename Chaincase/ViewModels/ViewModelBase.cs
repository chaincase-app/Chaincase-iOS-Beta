using System.Threading.Tasks;
using ReactiveUI;
using Chaincase.Navigation;

namespace Chaincase.ViewModels
{
	public class ViewModelBase : ReactiveObject, IRoutableViewModel
	{
		protected readonly IScreen _viewStackService;
		public IScreen HostScreen => _viewStackService;

		public string UrlPathSegment => this.GetType().BaseType.Name.Replace("ViewModel", "");

		public ViewModelBase(IScreen viewStackService)
		{
			_viewStackService = viewStackService;
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
