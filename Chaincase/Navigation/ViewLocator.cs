using ReactiveUI;
using System;

namespace Chaincase.Navigation
{
	public class ViewLocator : IViewLocator
	{
		public IViewFor ResolveView<T>(T viewModel, string contract = null) where T : class
		{
			var viewType = FindPageForViewModel(viewModel.GetType());

			var viewFor = (IViewFor)Activator.CreateInstance(viewType);

			viewFor.ViewModel = viewModel;

			return viewFor;
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
