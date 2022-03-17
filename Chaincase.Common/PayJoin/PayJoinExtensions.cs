using System;
using Microsoft.Extensions.DependencyInjection;

namespace Chaincase.Common.PayJoin
{
    public static class PayJoinExtensions
    {
        public static void AddPayJoinServices(this IServiceCollection services)
        {
            services.AddSingleton<P2EPServer>();
        }
    }
}
