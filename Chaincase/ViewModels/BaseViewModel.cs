using System.Threading.Tasks;
using ReactiveUI;
using Chaincase.Navigation;

namespace Chaincase.ViewModels
{
	public class BaseViewModel : ReactiveObject
	{
		protected readonly IViewStackService ViewStackService;

		public BaseViewModel(IViewStackService viewStackService)
		{
			ViewStackService = viewStackService;
		}

		public string UrlPathSegment => this.GetType().BaseType.Name.Replace("ViewModel", "");
	}
}
