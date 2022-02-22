using BTCPayServer.BIP78.Sender;
using Chaincase.Common.PayJoin.Sender;
using Microsoft.Extensions.DependencyInjection;
using Chaincase.Common.Services;

namespace Chaincase.Common.PayJoin
{
    public static class PayJoinExtensions
    {
        public static void AddPayJoinServices(this IServiceCollection services)
        {
            services.AddSingleton<IPayjoinServerCommunicator, PayjoinServerCommunicator>();
            services.AddSingleton<PayjoinClient>();
            services.AddTransient<Socks5HttpClientHandler>();
            services.AddHttpClient(PayjoinServerCommunicator.PayjoinOnionNamedClient)
                .ConfigurePrimaryHttpMessageHandler<Socks5HttpClientHandler>();
        }
    }
}