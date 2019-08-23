using System;
using Xamarin.Forms;

namespace Wasabi.ViewModels
{
	public interface IViewLocator
	{
		Page CreateAndBindPageFor<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase;
	}

	public class ViewLocator : IViewLocator
	{
		public Page CreateAndBindPageFor<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase
		{
			var pageType = FindPageForViewModel(viewModel.GetType());

			var page = (Page)Activator.CreateInstance(pageType);

			page.BindingContext = viewModel;

			return page;
		}

		protected virtual Type FindPageForViewModel(Type viewModelType)
		{
			var pageTypeName = viewModelType
				.AssemblyQualifiedName
				.Replace("ViewModels", "Views")
				.Replace("ViewModel", "Page");

			var pageType = Type.GetType(pageTypeName);
			if (pageType == null)
				throw new ArgumentException(pageTypeName + " type does not exist");

			return pageType;
		}
	}
}
