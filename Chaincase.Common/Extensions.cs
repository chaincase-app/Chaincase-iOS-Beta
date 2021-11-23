using System;
using System.Collections.Generic;
using System.IO;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
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
            services.AddScoped<SensitiveStorage>();
            services.AddSingleton(x => {
                var network = x.GetRequiredService<Config>().Network;
                var dataDir = x.GetRequiredService<IDataDirProvider>().Get();
                var walletDirectories = x.GetService<WalletDirectories>() ?? new WalletDirectories(dataDir);
                var notificationManager = x.GetRequiredService<INotificationManager>();
                return new ChaincaseWalletManager(network, walletDirectories, notificationManager);
            });
            services.AddSingleton(x =>
            {
                var network = x.GetRequiredService<Config>().Network;
                
                
                var client = x.GetRequiredService<ChaincaseClient>();
                var dataDir = x.GetRequiredService<IDataDirProvider>().Get();
                
                var networkWorkFolderPath = Path.Combine(dataDir, network.ToString());
                var indexStoreFolderPath = Path.Combine(networkWorkFolderPath, "IndexStore");
                var bitcoinStoreFolderPath = Path.Combine(dataDir, "BitcoinStore");
                var txStorepath = Path.Combine(bitcoinStoreFolderPath, "IndexStore");
                var indexStore = new IndexStore(indexStoreFolderPath,network, new SmartHeaderChain());

                return new ChaincaseBitcoinStore(indexStore, new AllTransactionStore(txStorepath, network), new MempoolService(),client);
            });
            services.AddSingleton<BitcoinStore>(provider => provider.GetRequiredService<ChaincaseBitcoinStore>());
            services.AddSingleton(provider => provider.GetRequiredService<Global>().BlockProvider);
            services.AddSingleton(provider => provider.GetRequiredService<Global>().BlockRepository);
            services.AddSingleton(x =>
            {
                var config = x.GetRequiredService<Config>();
                var network = config.Network;
                var bitcoinStore = x.GetRequiredService<BitcoinStore>();

                if (config.UseTor)
                    return new ChaincaseSynchronizer(network, bitcoinStore, () => config.GetCurrentBackendUri(), config.TorSocks5EndPoint);

                return new ChaincaseSynchronizer(network, bitcoinStore, config.GetFallbackBackendUri(), null);
            });
            services.AddSingleton(x =>
            {
                return new FeeProviders(new List<IFeeProvider>
				{
                    x.GetRequiredService<ChaincaseSynchronizer>()
                });
            });
            services.AddSingleton(x =>
            {
	            var config = x.GetRequiredService<Config>();
	            return new ChaincaseClient(config.GetCurrentBackendUri, config.TorSocks5EndPoint);
            });
            

            return services;
        }
    }
}
