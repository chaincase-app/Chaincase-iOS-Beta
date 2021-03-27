using System.IO;
using Chaincase.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WalletWasabi.Blockchain.Blocks;
using WalletWasabi.Blockchain.Mempool;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Stores;
using WalletWasabi.Wallets;

namespace Chaincase.Common
{
	public static class Extensions
    {
        public static IServiceCollection AddCommonServices(this IServiceCollection services)
        {
            services.AddSingleton<Global>();
            services.AddSingleton<Config>();
            services.AddSingleton<UiConfig>();
            services.AddSingleton<WalletManager>();
            services.AddSingleton(x =>
            {
                var config = x.GetRequiredService<Config>();
                var dataDir = x.GetRequiredService<IDataDirProvider>().Get();
                var indexStore = new IndexStore(config.Network, new SmartHeaderChain());

                return new BitcoinStore(Path.Combine(dataDir, "BitcoinStore"), config.Network,
                    indexStore, new AllTransactionStore(), new MempoolService()
                );
            });

            return services;
        }
    }
}
