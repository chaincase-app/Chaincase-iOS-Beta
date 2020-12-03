using Microsoft.Extensions.DependencyInjection;

namespace Chaincase.UI.Services
{
	public static class Extensions
	{
		public static IServiceCollection AddUIServices(this IServiceCollection collection)
		{
			collection.AddSingleton<UIStateService>();
			return collection;
		}
	}
}
