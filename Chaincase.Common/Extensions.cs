using Microsoft.Extensions.DependencyInjection;

namespace Chaincase.Common
{
	public static class Extensions
    {
        public static IServiceCollection AddCommonServices(this IServiceCollection services)
        {
            services.AddSingleton<Global>();
            services.AddSingleton<Config>();
            services.AddSingleton<UiConfig>();

            return services;
        }
    }
}
