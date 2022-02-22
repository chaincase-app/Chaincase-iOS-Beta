using System;
using System.Collections.Generic;
using System.IO;
using Chaincase.Common.Contracts;
using Chaincase.Common.PayJoin;
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
            services.AddSingleton(x =>
            {
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
                var indexStore = new IndexStore(network, new SmartHeaderChain());

                return new ChaincaseBitcoinStore(Path.Combine(dataDir, "BitcoinStore"), network,
                    indexStore, new AllTransactionStore(), new MempoolService(), client
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
            services.AddPayJoinServices();

            return services;
        }


        public static bool IsOnion(this Uri uri)
        {
            if (uri == null || !uri.IsAbsoluteUri)
                return false;
            return uri.DnsSafeHost.EndsWith(".onion", StringComparison.OrdinalIgnoreCase);
        }
    }
}
