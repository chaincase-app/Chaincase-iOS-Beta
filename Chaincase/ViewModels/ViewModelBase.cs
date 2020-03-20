using System.Threading.Tasks;
using ReactiveUI;
using Chaincase.Navigation;

namespace Chaincase.ViewModels
{
	public class ViewModelBase : ReactiveObject, IViewModel
	{
		protected readonly IViewStackService ViewStackService;

		public ViewModelBase(IViewStackService viewStackService)
		{
			ViewStackService = viewStackService;
		}

		public string UrlPathSegment => this.GetType().BaseType.Name.Replace("ViewModel", "");

        string IViewModel.Id
        {
			get;
        }
    }
}
