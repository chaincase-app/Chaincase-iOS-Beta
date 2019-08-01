using System;
using System.Threading.Tasks;

namespace Wasabi.Navigation
{
	// https://mallibone.com/post/a-simple-navigation-service-for-xamarinforms
	public interface INavigationService
	{
		string CurrentPageKey { get; }

		void Configure(string pageKey, Type pageType);
		Task GoBack();
		Task NavigateModalAsync(string pageKey, bool animated = true);
		Task NavigateModalAsync(string pageKey, object parameter, bool animated = true);
		Task NavigateAsync(string pageKey, bool animated = true);
		Task NavigateAsync(string pageKey, object parameter, bool animated = true);
	}
}
