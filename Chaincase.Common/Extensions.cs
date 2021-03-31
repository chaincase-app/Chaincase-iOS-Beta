using System.IO;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
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
            // WalletManager = new WalletManager(Network, new WalletDirectories(DataDir));

            services.AddSingleton(x => {
                var network = x.GetRequiredService<Config>().Network;
                var dataDir = x.GetRequiredService<IDataDirProvider>().Get();
                return new ChaincaseWalletManager(network, new WalletDirectories(dataDir));
            });
            services.AddSingleton(x =>
            {
                var network = x.GetRequiredService<Config>().Network;
                var dataDir = x.GetRequiredService<IDataDirProvider>().Get();
                var indexStore = new IndexStore(network, new SmartHeaderChain());

                return new BitcoinStore(Path.Combine(dataDir, "BitcoinStore"), network,
                    indexStore, new AllTransactionStore(), new MempoolService()
                );
            });

            return services;
        }
    }
}
