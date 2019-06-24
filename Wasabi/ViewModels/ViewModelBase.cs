using Wasabi.Navigation;

namespace Wasabi.ViewModels
{
	public class ViewModelBase : ExtendedBindableObject
	{
		protected readonly INavigationService _navigationService;

		public ViewModelBase(INavigationService navigationService)
		{
			_navigationService = navigationService;
		}
	}
}
