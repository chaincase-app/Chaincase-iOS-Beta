using System;
using System.Collections.Generic;
using System.IO;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<MempoolSynchronizer>();
            services.AddHostedService(provider => provider.GetRequiredService<MempoolSynchronizer>());
            services.AddSingleton(x => {
                var network = x.GetRequiredService<Config>().Network;
                var dataDir = x.GetRequiredService<IDataDirProvider>().Get();
                var walletDirectories = x.GetService<WalletDirectories>() ?? new WalletDirectories(dataDir);
                var notificationManager = x.GetRequiredService<INotificationManager>();
                var mempoolSynchronizer = x.GetRequiredService<MempoolSynchronizer>();
                return new ChaincaseWalletManager(network, walletDirectories, notificationManager, mempoolSynchronizer);
            });
            services.AddSingleton(x =>
            {
                var network = x.GetRequiredService<Config>().Network;
                var client = x.GetRequiredService<ChaincaseClient>();
                var dataDir = x.GetRequiredService<IDataDirProvider>().Get();
                var indexStore = new IndexStore(network, new SmartHeaderChain());

                return new ChaincaseBitcoinStore(Path.Combine(dataDir, "BitcoinStore"), network,
                    indexStore, new AllTransactionStore(), new MempoolService(),client
                );
            });
            services.AddSingleton<BitcoinStore>(provider => provider.GetRequiredService<ChaincaseBitcoinStore>());
            services.AddSingleton(x =>
            {
                var config = x.GetRequiredService<Config>();
                var network = config.Network;
                var bitcoinStore = x.GetRequiredService<BitcoinStore>();

                if (config.UseTor)
                    return new ChaincaseSynchronizer(network, bitcoinStore, () => config.GetCurrentBackendUri(), config.TorSocks5EndPoint);

                return new ChaincaseSynchronizer(network, bitcoinStore, config.GetFallbackBackendUri(), null);
            });
            services.AddSingleton<ChaincaseClient>();
            services.AddSingleton<IFeeProvider, ChaincaseSynchronizer>();
            services.AddSingleton<FeeProviders>();
            

            return services;
        }
    }
}
