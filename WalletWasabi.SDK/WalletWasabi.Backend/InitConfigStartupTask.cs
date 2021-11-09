using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Backend.Data;
using WalletWasabi.Backend.Polyfills;
using WalletWasabi.BitcoinCore;
using WalletWasabi.CoinJoin.Coordinator.Rounds;
using WalletWasabi.Logging;

namespace WalletWasabi.Backend
{
	public class InitConfigStartupTask : IStartupTask
	{
		private readonly WebsiteTorifier _websiteTorifier;
		private readonly IServiceProvider _serviceProvider;

		public InitConfigStartupTask(Global global, IMemoryCache cache, WebsiteTorifier websiteTorifier, IServiceProvider serviceProvider )
		{
			_websiteTorifier = websiteTorifier;
			_serviceProvider = serviceProvider;
			Global = global;
			Cache = cache;
		}
		public Global Global { get; }
		public IMemoryCache Cache { get; }
		public IDbContextFactory<WasabiBackendContext> ContextFactory { get; }

		public async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			Logger.InitializeDefaults(Path.Combine(Global.DataDir, "Logs.txt"));
			Logger.LogSoftwareStarted("Wasabi Backend");

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
			var configFilePath = Path.Combine(Global.DataDir, "Config.json");
			var config = new Config(configFilePath);
			config.LoadOrCreateDefaultFile();
			Logger.LogInfo("Config is successfully initialized.");

			var roundConfigFilePath = Path.Combine(Global.DataDir, "CcjRoundConfig.json");
			var roundConfig = new CoordinatorRoundConfig(roundConfigFilePath);
			roundConfig.LoadOrCreateDefaultFile();
			Logger.LogInfo("RoundConfig is successfully initialized.");

			string host = config.GetBitcoinCoreRpcEndPoint().ToString(config.Network.RPCPort);
			var rpc = new RPCClient(
					authenticationString: config.BitcoinRpcConnectionString,
					hostOrUri: host,
					network: config.Network);

			var cachedRpc = new CachedRpcClient(rpc, Cache);
			await Global.InitializeAsync(config, roundConfig, cachedRpc, _serviceProvider, cancellationToken);

			try
			{
				await _websiteTorifier.CloneAndUpdateOnionIndexHtmlAsync();
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex);
			}
		}

		private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			Logger.LogWarning(e?.Exception);
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Logger.LogWarning(e?.ExceptionObject as Exception);
		}
	}
}
