using Microsoft.Extensions.DependencyInjection;

namespace Chaincase.Common.Xamarin
{
    public static class XamarinExtensions
    {
        public static void ConfigureCommonXamarinServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IHsmStorage, XamarinHsmStorage>();
        }
    }
}