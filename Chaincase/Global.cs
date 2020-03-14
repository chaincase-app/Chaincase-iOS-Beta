using Chaincase;
using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.Protocol.Connectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Blockchain.TransactionBroadcasting;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.CoinJoin.Client;
using WalletWasabi.CoinJoin.Client.Clients;
using WalletWasabi.CoinJoin.Client.Clients.Queuing;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.Services;
using WalletWasabi.Stores;
using Xamarin.Forms;

namespace Chaincase
{
    public static class Global
	{
		public static string DataDir { get; }
		public static string TorLogsFile { get; }
		public static string WalletsDir { get; }
		public static string WalletBackupsDir { get; }

		public static BitcoinStore BitcoinStore { get; private set; }
		public static Config Config { get; private set; }

		public static string AddressManagerFilePath { get; private set; }
		public static AddressManager AddressManager { get; private set; }

		public static NodesGroup Nodes { get; private set; }
		public static WasabiSynchronizer Synchronizer { get; private set; }
        public static FeeProviders FeeProviders { get; private set; }
        public static CoinJoinClient ChaumianClient { get; private set; }
		public static WalletService WalletService { get; private set; }
        public static TransactionBroadcaster TransactionBroadcaster { get; set; }
        public static CoinJoinProcessor CoinJoinProcessor { get; set; }
		public static Node RegTestMempoolServingNode { get; private set; }
		public static ITorManager TorManager { get; private set; }

        public static bool KillRequested { get; private set; } = false;

		public static NBitcoin.Network Network => Config.Network;

		static Global()
		{
			DataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Chaincase", "Client"));
			TorLogsFile = Path.Combine(DataDir, "TorLogs.txt");
			WalletsDir = Path.Combine(DataDir, "Wallets");
			WalletBackupsDir = Path.Combine(DataDir, "WalletBackups");

			Directory.CreateDirectory(DataDir);
			Directory.CreateDirectory(WalletsDir);
			Directory.CreateDirectory(WalletBackupsDir);
        }

        private static int _isDesperateDequeuing = 0;

		public static async Task TryDesperateDequeueAllCoinsAsync()
		{
			// If already desperate dequeueing then return.
			// If not desperate dequeueing then make sure we're doing that.
			if (Interlocked.CompareExchange(ref _isDesperateDequeuing, 1, 0) == 1)
			{
				return;
			}
			try
			{
				await DesperateDequeueAllCoinsAsync();
			}
			catch (NotSupportedException ex)
			{
				Logger.LogWarning(ex.Message);
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex);
			}
			finally
			{
				Interlocked.Exchange(ref _isDesperateDequeuing, 0);
			}
		}

		public static async Task DesperateDequeueAllCoinsAsync()
		{
			if (WalletService is null || ChaumianClient is null)
			{
				return;
			}

            SmartCoin[] enqueuedCoins = WalletService.Coins.CoinJoinInProcess().ToArray();
            if (enqueuedCoins.Any())
			{
				Logger.LogWarning("Unregistering coins in CoinJoin process.");
                await ChaumianClient.DequeueCoinsFromMixAsync(enqueuedCoins, DequeueReason.ApplicationExit);
            }
		}

		private static bool InitializationCompleted { get; set; } = false;

        private static CancellationTokenSource StoppingCts { get; set; } = new CancellationTokenSource();

        public static async Task InitializeNoWalletAsync()
		{
            try
            {
                WalletService = null;
                ChaumianClient = null;
                AddressManager = null;
                TorManager = null;

                #region ConfigInitialization

                Config = new Config(Path.Combine(DataDir, "Config.json"));
                await Config.LoadOrCreateDefaultFileAsync();
                Logger.LogInfo($"{nameof(Config)} is successfully initialized.");

                #endregion ConfigInitialization

                BitcoinStore = new BitcoinStore();
                var bstoreInitTask = BitcoinStore.InitializeAsync(Path.Combine(DataDir, "BitcoinStore"), Network);

                var addressManagerFolderPath = Path.Combine(DataDir, "AddressManager");
                AddressManagerFilePath = Path.Combine(addressManagerFolderPath, $"AddressManager{Network}.dat");
                var addrManTask = Global.InitializeAddressManagerBehaviorAsync();

                var blocksFolderPath = Path.Combine(DataDir, $"Blocks{Network}");
                var connectionParameters = new NodeConnectionParameters();

                if (Config.UseTor)
                {
                    Synchronizer = new WasabiSynchronizer(Network, BitcoinStore, () => Config.GetCurrentBackendUri(), Config.TorSocks5EndPoint);
                }
                else
                {
                    Synchronizer = new WasabiSynchronizer(Network, BitcoinStore, Config.GetFallbackBackendUri(), null);
                }

#region ProcessKillSubscription

                AppDomain.CurrentDomain.ProcessExit += async (s, e) => await TryDesperateDequeueAllCoinsAsync();

#endregion ProcessKillSubscription

#region TorProcessInitialization

                if (Config.UseTor)
                {
                    TorManager = DependencyService.Get<ITorManager>();
                    //TorManager = new TorProcessManager(Config.TorSocks5EndPoint, TorLogsFile);
                }
                else
                {
                    TorManager = DependencyService.Get<ITorManager>().Mock();
                }
                TorManager.Start(false, DataDir);

                var fallbackRequestTestUri = new Uri(Config.GetFallbackBackendUri(), "/api/software/versions");
                //TorManager.StartMonitor(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7), DataDir, fallbackRequestTestUri);

                Logger.LogInfo($"{nameof(TorManager)} is initialized.");

#endregion TorProcessInitialization

#region BitcoinStoreInitialization

                await bstoreInitTask;

#endregion BitcoinStoreInitialization

#region BitcoinCoreInitialization

                var feeProviderList = new List<IFeeProvider>
                {
                    Synchronizer
                    // wasabiwallet.io's implementation also gets fees from a core node initialized in this region
                };

                FeeProviders = new FeeProviders(feeProviderList);

#endregion BitcoinCoreInitialization

#region MempoolInitialization

                connectionParameters.TemplateBehaviors.Add(BitcoinStore.CreateUntrustedP2pBehavior());

#endregion MempoolInitialization

#region AddressManagerInitialization

                AddressManagerBehavior addressManagerBehavior = await addrManTask;
                connectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

#endregion AddressManagerInitialization

#region P2PInitialization

                if (Network == NBitcoin.Network.RegTest)
                {
                    Nodes = new NodesGroup(Network, requirements: Constants.NodeRequirements);
                    try
                    {
                        Node node = await Node.ConnectAsync(NBitcoin.Network.RegTest, new IPEndPoint(IPAddress.Loopback, 18444));
                        Nodes.ConnectedNodes.Add(node);

                        RegTestMempoolServingNode = await Node.ConnectAsync(NBitcoin.Network.RegTest, new IPEndPoint(IPAddress.Loopback, 18444));

                        RegTestMempoolServingNode.Behaviors.Add(BitcoinStore.CreateUntrustedP2pBehavior());
                    }
                    catch (SocketException ex)
                    {
                        Logger.LogError(ex);
                    }
                }
                else
                {
                    if (Config.UseTor is true)
                    {
                        // onlyForOnionHosts: false - Connect to clearnet IPs through Tor, too.
                        connectionParameters.TemplateBehaviors.Add(new SocksSettingsBehavior(Config.TorSocks5EndPoint, onlyForOnionHosts: false, networkCredential: null, streamIsolation: true));
                        // allowOnlyTorEndpoints: true - Connect only to onions and don't connect to clearnet IPs at all.
                        // This of course makes the first setting unneccessary, but it's better if that's around, in case someone wants to tinker here.
                        connectionParameters.EndpointConnector = new DefaultEndpointConnector(allowOnlyTorEndpoints: Network == NBitcoin.Network.Main);

                        await AddKnownBitcoinFullNodeAsHiddenServiceAsync(AddressManager);
                    }
                    Nodes = new NodesGroup(Network, connectionParameters, requirements: Constants.NodeRequirements);

                    RegTestMempoolServingNode = null;
                }

                Nodes.Connect();
                Logger.LogInfo("Start connecting to nodes...");

                var regTestMempoolServingNode = RegTestMempoolServingNode;
                if (regTestMempoolServingNode != null)
                {
                    regTestMempoolServingNode.VersionHandshake();
                    Logger.LogInfo("Start connecting to mempool serving regtest node...");
                }

#endregion P2PInitialization

#region SynchronizerInitialization

                var requestInterval = TimeSpan.FromSeconds(30);
                if (Network == NBitcoin.Network.RegTest)
                {
                    requestInterval = TimeSpan.FromSeconds(5);
                }

                int maxFiltSyncCount = Network == NBitcoin.Network.Main ? 1000 : 10000; // On testnet, filters are empty, so it's faster to query them together

                Synchronizer.Start(requestInterval, TimeSpan.FromMinutes(5), maxFiltSyncCount);
                Logger.LogInfo("Start synchronizing filters...");

#endregion SynchronizerInitialization

                TransactionBroadcaster = new TransactionBroadcaster(Network, BitcoinStore, Synchronizer, Nodes, null);
                CoinJoinProcessor = new CoinJoinProcessor(Synchronizer, null);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex);
                InitializationCompleted = true;
                await DisposeAsync().ConfigureAwait(false);
                Environment.Exit(1);
            }
            finally
            {
                InitializationCompleted = true;
            }
		}

        private static async Task<AddressManagerBehavior> InitializeAddressManagerBehaviorAsync()
        {
            var needsToDiscoverPeers = true;
            if (Network == NBitcoin.Network.RegTest)
            {
                AddressManager = new AddressManager();
                Logger.LogInfo($"Fake {nameof(AddressManager)} is initialized on the RegTest.");
            }
            else
            {
                try
                {
                    AddressManager = await NBitcoinHelpers.LoadAddressManagerFromPeerFileAsync(AddressManagerFilePath);

                    // The most of the times we don't need to discover new peers. Instead, we can connect to
                    // some of those that we already discovered in the past. In this case we assume that we
                    // assume that discovering new peers could be necessary if out address manager has less
                    // than 500 addresses. A 500 addresses could be okay because previously we tried with
                    // 200 and only one user reported he/she was not able to connect (there could be many others,
                    // of course).
                    // On the other side, increasing this number forces users that do not need to discover more peers
                    // to spend resources (CPU/bandwith) to discover new peers.
                    needsToDiscoverPeers = Config.UseTor == true || AddressManager.Count < 500;
                    Logger.LogInfo($"Loaded {nameof(AddressManager)} from `{AddressManagerFilePath}`.");
                }
                catch (DirectoryNotFoundException ex)
                {
                    Logger.LogInfo($"{nameof(AddressManager)} did not exist at `{AddressManagerFilePath}`. Initializing new one.");
                    Logger.LogTrace(ex);
                    AddressManager = new AddressManager();
                }
                catch (FileNotFoundException ex)
                {
                    Logger.LogInfo($"{nameof(AddressManager)} did not exist at `{AddressManagerFilePath}`. Initializing new one.");
                    Logger.LogTrace(ex);
                    AddressManager = new AddressManager();
                }
                catch (OverflowException ex)
                {
                    // https://github.com/zkSNACKs/WalletWasabi/issues/712
                    Logger.LogInfo($"{nameof(AddressManager)} has thrown `{nameof(OverflowException)}`. Attempting to autocorrect.");
                    File.Delete(AddressManagerFilePath);
                    Logger.LogTrace(ex);
                    AddressManager = new AddressManager();
                    Logger.LogInfo($"{nameof(AddressManager)} autocorrection is successful.");
                }
                catch (FormatException ex)
                {
                    // https://github.com/zkSNACKs/WalletWasabi/issues/880
                    Logger.LogInfo($"{nameof(AddressManager)} has thrown `{nameof(FormatException)}`. Attempting to autocorrect.");
                    File.Delete(AddressManagerFilePath);
                    Logger.LogTrace(ex);
                    AddressManager = new AddressManager();
                    Logger.LogInfo($"{nameof(AddressManager)} autocorrection is successful.");
                }
            }

            var addressManagerBehavior = new AddressManagerBehavior(AddressManager)
            {
                Mode = needsToDiscoverPeers ? AddressManagerBehaviorMode.Discover : AddressManagerBehaviorMode.None
            };
            return addressManagerBehavior;
        }

        private static async Task AddKnownBitcoinFullNodeAsHiddenServiceAsync(AddressManager addressManager)
		{
			if (Network == NBitcoin.Network.RegTest)
			{
				return;
			}

            //  curl -s https://bitnodes.21.co/api/v1/snapshots/latest/ | egrep -o '[a-z0-9]{16}\.onion:?[0-9]*' | sort -ru
            // Then filtered to include only /Satoshi:0.17.x
            var fullBaseDirectory = EnvironmentHelpers.GetFullBaseDirectory();

			var onions = await File.ReadAllLinesAsync(Path.Combine(fullBaseDirectory, "OnionSeeds", $"{Network}OnionSeeds.txt"));

			onions.Shuffle();
			foreach (var onion in onions.Take(60))
			{
				if (EndPointParser.TryParse(onion, Network.DefaultPort, out var endpoint))
				{
					await addressManager.AddAsync(endpoint);
				}
			}
		}

		private static CancellationTokenSource _cancelWalletServiceInitialization = null;

		public static async Task InitializeWalletServiceAsync(KeyManager keyManager)
		{
            using(_cancelWalletServiceInitialization = new CancellationTokenSource())
            {
                var token = _cancelWalletServiceInitialization.Token;
                while (!InitializationCompleted)
                {
                    await Task.Delay(100);
                }

                if (Config.UseTor)
                {
                    ChaumianClient = new CoinJoinClient(Synchronizer, Network, keyManager, () => Config.GetCurrentBackendUri(), Config.TorSocks5EndPoint);
                }
                else
                {
                    ChaumianClient = new CoinJoinClient(Synchronizer, Network, keyManager, Config.GetFallbackBackendUri(), null);
                }

                WalletService = new WalletService(BitcoinStore, keyManager, Synchronizer, ChaumianClient, Nodes, DataDir, Config.ServiceConfiguration, FeeProviders);

                ChaumianClient.Start();
                Logger.LogInfo("Start Chaumian CoinJoin service...");

                Logger.LogInfo($"Starting {nameof(WalletService)}...");
                await WalletService.InitializeAsync(token);
                Logger.LogInfo($"{nameof(WalletService)} started.");

                token.ThrowIfCancellationRequested();

                TransactionBroadcaster.AddWalletService(WalletService);
                CoinJoinProcessor.AddWalletService(WalletService);

                // TODO ui doesn't support notifications to handle these events yet:
                // WalletService.TransactionProcessor.WalletRelevantTransactionProcessed += TransactionProcessor_WalletRelevantTransactionProcessed;
                // ChaumianClient.OnDequeue += ChaumianClient_OnDequeued;
            }
			_cancelWalletServiceInitialization = null; // Must make it null explicitly, because dispose won't make it null.
		}

        public static string GetWalletFullPath(string walletName)
		{
			walletName = walletName.TrimEnd(".json", StringComparison.OrdinalIgnoreCase);
			return Path.Combine(WalletsDir, walletName + ".json");
		}

		public static string GetWalletBackupFullPath(string walletName)
		{
			walletName = walletName.TrimEnd(".json", StringComparison.OrdinalIgnoreCase);
			return Path.Combine(WalletBackupsDir, walletName + ".json");
		}

		public static KeyManager LoadKeyManager(string walletFullPath, string walletBackupFullPath)
		{
			try
			{
				return LoadKeyManager(walletFullPath);
			}
			catch (Exception ex)
			{
				if (!File.Exists(walletBackupFullPath))
				{
					throw;
				}

				Logger.LogWarning($"Wallet got corrupted.\n" +
					$"Wallet Filepath: {walletFullPath}\n" +
					$"Trying to recover it from backup.\n" +
					$"Backup path: {walletBackupFullPath}\n" +
					$"Exception: {ex.ToString()}");
				if (File.Exists(walletFullPath))
				{
					string corruptedWalletBackupPath = Path.Combine(WalletBackupsDir, $"{Path.GetFileName(walletFullPath)}_CorruptedBackup");
					if (File.Exists(corruptedWalletBackupPath))
					{
						File.Delete(corruptedWalletBackupPath);
						Logger.LogInfo($"Deleted previous corrupted wallet file backup from {corruptedWalletBackupPath}.");
					}
					File.Move(walletFullPath, corruptedWalletBackupPath);
					Logger.LogInfo($"Backed up corrupted wallet file to {corruptedWalletBackupPath}.");
				}
				File.Copy(walletBackupFullPath, walletFullPath);

				return LoadKeyManager(walletFullPath);
			}
		}

		public static KeyManager LoadKeyManager(string walletFullPath)
		{
			KeyManager keyManager;

			// Set the LastAccessTime.
			new FileInfo(walletFullPath)
			{
				LastAccessTime = DateTime.Now
			};

			keyManager = KeyManager.FromFile(walletFullPath);
			Logger.LogInfo($"Wallet loaded: {Path.GetFileNameWithoutExtension(keyManager.FilePath)}.");
			return keyManager;
		}

		public static async Task DisposeInWalletDependentServicesAsync()
		{
			if (WalletService != null)
			{
                // WalletService.TransactionProcessor.WalletRelevantTransactionProcessed -= TransactionProcessor_WalletRelevantTransactionProcessed;
                // ChaumianClient.OnDequeue -= ChaumianClient_OnDequeued;
            }
			try
			{
				_cancelWalletServiceInitialization?.Cancel();
			}
			catch (ObjectDisposedException)
			{
				Logger.LogWarning($"{nameof(_cancelWalletServiceInitialization)} is disposed. This can occur due to an error while processing the wallet.");
			}
			_cancelWalletServiceInitialization = null;

			if (WalletService != null)
			{
				if (WalletService.KeyManager != null) // This should not ever happen.
				{
					string backupWalletFilePath = Path.Combine(WalletBackupsDir, Path.GetFileName(WalletService.KeyManager.FilePath));
					WalletService.KeyManager?.ToFile(backupWalletFilePath);
					Logger.LogInfo($"{nameof(KeyManager)} backup saved to {backupWalletFilePath}.");
				}
				WalletService?.Dispose();
				WalletService = null;
				Logger.LogInfo($"{nameof(WalletService)} is stopped.");
			}

			if (ChaumianClient != null)
			{
				await ChaumianClient.StopAsync();
				ChaumianClient = null;
				Logger.LogInfo($"{nameof(ChaumianClient)} is stopped.");
			}
		}

		public static async Task DisposeAsync()
		{
			try
			{
				await DisposeInWalletDependentServicesAsync();

                var feeProviders = FeeProviders;
                if (feeProviders != null)
                {
                    feeProviders.Dispose();
                    Logger.LogInfo($"Disposed {nameof(FeeProviders)}.");
                }
                var synchronizer = Synchronizer;
                if (synchronizer is { })
                {
                    await synchronizer.StopAsync();
                    Logger.LogInfo($"{nameof(Synchronizer)} is stopped.");
                }

                var addressManagerFilePath = AddressManagerFilePath;
                if (addressManagerFilePath is { })
				{
					IoHelpers.EnsureContainingDirectoryExists(AddressManagerFilePath);
                    var addressManager = AddressManager;
                    if (addressManager is { })
					{
						addressManager?.SavePeerFile(AddressManagerFilePath, Config.Network);
						Logger.LogInfo($"{nameof(AddressManager)} is saved to `{AddressManagerFilePath}`.");
					}
				}

                var nodes = Nodes;
                if (nodes != null)
				{
					nodes?.Dispose();
					Logger.LogInfo($"{nameof(Nodes)} are disposed.");
				}

                var regTestMempoolServingNode = RegTestMempoolServingNode;
                if (regTestMempoolServingNode is { })
				{
					regTestMempoolServingNode.Disconnect();
					Logger.LogInfo($"{nameof(RegTestMempoolServingNode)} is disposed.");
				}

                var torManager = TorManager;
                if (torManager != null)
				{
					torManager?.Stop();
					Logger.LogInfo($"{nameof(TorManager)} is stopped.");
				}
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex);
			}
		}
	}
}
