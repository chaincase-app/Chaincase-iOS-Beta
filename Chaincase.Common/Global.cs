using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Microsoft.Extensions.Caching.Memory;
using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.Protocol.Connectors;
using Nito.AsyncEx;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using WalletWasabi.Blockchain.TransactionBroadcasting;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.TransactionProcessing;
using WalletWasabi.CoinJoin.Client;
using WalletWasabi.CoinJoin.Client.Clients.Queuing;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.Stores;
using WalletWasabi.Wallets;

namespace Chaincase.Common
{
    public class Global
    {
        private readonly Config Config;
        private Network Network => Config.Network;
        private readonly UiConfig UiConfig;
        private readonly BitcoinStore BitcoinStore;
        private readonly ChaincaseWalletManager WalletManager;
        private readonly INotificationManager NotificationManager;
        private readonly ITorManager TorManager;
        private readonly IDataDirProvider DataDirProvider;
        private string DataDir => DataDirProvider.Get();
        private CoinJoinProcessor CoinJoinProcessor;

        public string AddressManagerFilePath { get; private set; }
        public AddressManager AddressManager { get; private set; }

        public NodesGroup Nodes { get; private set; }
        public ChaincaseSynchronizer Synchronizer { get; private set; }
        public FeeProviders FeeProviders { get; private set; }
        public TransactionBroadcaster TransactionBroadcaster { get; set; }

        /// <summary>
        /// Responsible for keeping one life cycle event in critical sections at once
        /// </summary>
        private readonly AsyncLock LifeCycleMutex = new AsyncLock();
        private readonly AsyncLock SleepMutex = new AsyncLock();

        public event EventHandler<AppInitializedEventArgs> Initialized = delegate { };

        public bool IsInitialized { get; private set; } = false;

        private bool InitializationCompleted { get; set; } = false;

        private bool InitializationStarted { get; set; } = false;

        private CancellationTokenSource StoppingCts { get; set; }
        // Chaincase Specific
        private protected CancellationTokenSource SleepCts { get; set; } = new CancellationTokenSource();
        private protected bool IsGoingToSleep = false;

        public Global(ITorManager torManager, IDataDirProvider dataDirProvider, Config config, UiConfig uiConfig, ChaincaseWalletManager walletManager, BitcoinStore bitcoinStore)
        {
            TorManager = torManager;
            DataDirProvider = dataDirProvider;
            Config = config;
            UiConfig = uiConfig;
            WalletManager = walletManager;
            BitcoinStore = bitcoinStore;
            using (BenchmarkLogger.Measure())
            {
                StoppingCts = new CancellationTokenSource();
                Directory.CreateDirectory(DataDir);

                Logger.InitializeDefaults(Path.Combine(DataDir, "Logs.txt"));

                UiConfig.LoadOrCreateDefaultFile();
                Config.LoadOrCreateDefaultFile();
            }
        }

        public async Task InitializeNoWalletAsync(CancellationToken cancellationToken = default)
        {
            AddressManager = null;
            Logger.LogDebug($"Global.InitializeNoWalletAsync(): Waiting for a lock");
            try
            {
                InitializationStarted = true;

                Logger.LogDebug($"Global.InitializeNoWalletAsync(): Got lock");

                MemoryCache Cache = new MemoryCache(new MemoryCacheOptions
                {
                    SizeLimit = 1_000,
                    ExpirationScanFrequency = TimeSpan.FromSeconds(30)
                });
                var bstoreInitTask = BitcoinStore.InitializeAsync();
                var addressManagerFolderPath = Path.Combine(DataDir, "AddressManager");

                AddressManagerFilePath = Path.Combine(addressManagerFolderPath, $"AddressManager{Network}.dat");
                var addrManTask = InitializeAddressManagerBehaviorAsync();

                var blocksFolderPath = Path.Combine(DataDir, $"Blocks{Network}");
                var userAgent = Constants.UserAgents.RandomElement();
                var connectionParameters = new NodeConnectionParameters { UserAgent = userAgent };

                if (Config.UseTor)
                {
                    Synchronizer = new ChaincaseSynchronizer(Network, BitcoinStore, () => Config.GetCurrentBackendUri(), Config.TorSocks5EndPoint);
                }
                else
                {
                    Synchronizer = new ChaincaseSynchronizer(Network, BitcoinStore, Config.GetFallbackBackendUri(), null);
                }

                #region TorProcessInitialization

                if (Config.UseTor)
                {
                    await TorManager.StartAsync(ensureRunning: false, DataDir);
                    Logger.LogInfo($"{nameof(TorManager)} is initialized.");
                }

                Logger.LogInfo($"Global.InitializeNoWalletAsync():{nameof(TorManager)} is initialized.");

                #endregion TorProcessInitialization

                #region BitcoinStoreInitialization

                await bstoreInitTask.ConfigureAwait(false);

                // Make sure that the height of the wallets will not be better than the current height of the filters.
                WalletManager.SetMaxBestHeight(BitcoinStore.IndexStore.SmartHeaderChain.TipHeight);

                #endregion BitcoinStoreInitialization

                #region FeeProviderInitialization
                // Mirrors #region BitcoinCoreInitialization in WalletWasabi

                var feeProviderList = new List<IFeeProvider>
            {
                Synchronizer
            };

                FeeProviders = new FeeProviders(feeProviderList);

                #endregion FeeProviderInitialization

                #region MempoolInitialization

                connectionParameters.TemplateBehaviors.Add(BitcoinStore.CreateUntrustedP2pBehavior());

                #endregion MempoolInitialization

                #region AddressManagerInitialization

                AddressManagerBehavior addressManagerBehavior = await addrManTask.ConfigureAwait(false);
                connectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

                #endregion AddressManagerInitialization

                #region P2PInitialization
                Node regTestMempoolServingNode = null;
                if (Network == Network.RegTest)
                {
                    Nodes = new NodesGroup(Network, requirements: Constants.NodeRequirements);
                    try
                    {
                        EndPoint bitcoinCoreEndpoint = Config.GetBitcoinP2pEndPoint();

                        Node node = await Node.ConnectAsync(NBitcoin.Network.RegTest, bitcoinCoreEndpoint).ConfigureAwait(false);

                        Nodes.ConnectedNodes.Add(node);

                        regTestMempoolServingNode = await Node.ConnectAsync(NBitcoin.Network.RegTest, bitcoinCoreEndpoint).ConfigureAwait(false);

                        regTestMempoolServingNode.Behaviors.Add(BitcoinStore.CreateUntrustedP2pBehavior());
                    }
                    catch (SocketException ex)
                    {
                        Logger.LogError(ex);
                    }
                }
                else
                {
                    if (Config.UseTor)
                    {
                        // onlyForOnionHosts: false - Connect to clearnet IPs through Tor, too.
                        connectionParameters.TemplateBehaviors.Add(new SocksSettingsBehavior(Config.TorSocks5EndPoint, onlyForOnionHosts: false, networkCredential: null, streamIsolation: true));
                        // allowOnlyTorEndpoints: true - Connect only to onions and don't connect to clearnet IPs at all.
                        // This of course makes the first setting unneccessary, but it's better if that's around, in case someone wants to tinker here.
                        connectionParameters.EndpointConnector = new DefaultEndpointConnector(allowOnlyTorEndpoints: Network == Network.Main);

                        await AddKnownBitcoinFullNodeAsHiddenServiceAsync(AddressManager).ConfigureAwait(false);
                    }
                    Nodes = new NodesGroup(Network, connectionParameters, requirements: Constants.NodeRequirements);
                    Nodes.MaximumNodeConnection = 12;
                    regTestMempoolServingNode = null;
                }

                Nodes.Connect();
                Logger.LogInfo("Global.InitializeNoWalletAsync(): Start connecting to nodes...");

                if (regTestMempoolServingNode != null)
                {
                    regTestMempoolServingNode.VersionHandshake();
                    Logger.LogInfo("Global.InitializeNoWalletAsync(): Start connecting to mempool serving regtest node...");
                }

                #endregion P2PInitialization

                #region SynchronizerInitialization

                var requestInterval = TimeSpan.FromSeconds(30);
                if (Network == Network.RegTest)
                {
                    requestInterval = TimeSpan.FromSeconds(5);
                }

                int maxFiltSyncCount = Network == Network.Main ? 1000 : 10000; // On testnet, filters are empty, so it's faster to query them together

                Synchronizer.Start(requestInterval, TimeSpan.FromMinutes(5), maxFiltSyncCount);
                Logger.LogInfo("Global.InitializeNoWalletAsync(): Start synchronizing filters...");

                #endregion SynchronizerInitialization

                TransactionBroadcaster = new TransactionBroadcaster(Network, BitcoinStore, Synchronizer, Nodes, WalletManager, null);
                // CoinJoinProcessor maintains an event handler to process joins.
                // We need this reference.
                CoinJoinProcessor = new CoinJoinProcessor(Synchronizer, WalletManager, null);

                #region Blocks provider

                var blockProvider = new CachedBlockProvider(
                    new SmartBlockProvider(
                        new P2pBlockProvider(Nodes, null, Synchronizer, Config.ServiceConfiguration, Network),
                        Cache),
                    new FileSystemBlockRepository(blocksFolderPath, Network));

                #endregion Blocks provider

                WalletManager.RegisterServices(BitcoinStore, Synchronizer, Nodes, Config.ServiceConfiguration, FeeProviders, blockProvider);

                Initialized(this, new AppInitializedEventArgs(this));
                IsInitialized = true;
            }
            finally
            {
                InitializationCompleted = true;
                StoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                Logger.LogDebug($"Initialization Completed");
            }
        }

        public async Task<AddressManagerBehavior> InitializeAddressManagerBehaviorAsync()
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
                    AddressManager = await NBitcoinHelpers.LoadAddressManagerFromPeerFileAsync(AddressManagerFilePath).ConfigureAwait(false);

                    // Most of the times we do not need to discover new peers. Instead, we can connect to
                    // some of those that we already discovered in the past. In this case we assume that
                    // discovering new peers could be necessary if our address manager has less
                    // than 500 addresses. 500 addresses could be okay because previously we tried with
                    // 200 and only one user reported he/she was not able to connect (there could be many others,
                    // of course).
                    // On the other side, increasing this number forces users that do not need to discover more peers
                    // to spend resources (CPU/bandwidth) to discover new peers.
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

        private async Task AddKnownBitcoinFullNodeAsHiddenServiceAsync(AddressManager addressManager)
        {
            if (Network == NBitcoin.Network.RegTest)
            {
                return;
            }

            //  curl -s https://bitnodes.21.co/api/v1/snapshots/latest/ | egrep -o '[a-z0-9]{16}\.onion:?[0-9]*' | sort -ru
            // Then filtered to include only /Satoshi:0.17.x
            var fullBaseDirectory = EnvironmentHelpers.GetFullBaseDirectory();

            var onions = await File.ReadAllLinesAsync(Path.Combine(fullBaseDirectory, "OnionSeeds", $"{Network}OnionSeeds.txt")).ConfigureAwait(false);

            onions.Shuffle();
            foreach (var onion in onions.Take(60))
            {
                if (EndPointParser.TryParse(onion, Network.DefaultPort, out var endpoint))
                {
                    await addressManager.AddAsync(endpoint).ConfigureAwait(false);
                }
            }

        }

        /// <returns>If initialization is successful, otherwise it was interrupted which means stopping was requested.</returns>
        public async Task<bool> WaitForInitializationCompletedAsync(CancellationToken cancellationToken)
        {
            while (!InitializationCompleted)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            return !StoppingCts.IsCancellationRequested;
        }

        public event EventHandler Resumed;
        private protected CancellationTokenSource ResumeCts { get; set; } = new CancellationTokenSource();
        private protected bool IsResuming { get; set; } = false;

        public async Task OnResuming()
        {
            if (IsResuming)
            {
                Logger.LogDebug($"Global.OnResuming(): SleepCts.Cancel()");
                SleepCts.Cancel();
                return;
            }

            try
            {
                Resumed?.Invoke(this, null);
                Logger.LogDebug($"Global.OnResuming(): Waiting for a lock");
                ResumeCts.Dispose();
                ResumeCts = new CancellationTokenSource();
                using (await LifeCycleMutex.LockAsync(ResumeCts.Token))
                {
                    #region CriticalSection
                    IsResuming = true;
                    Logger.LogDebug($"Global.OnResuming(): Entered critical section");

                    // don't ever cancel Init. use an ephemeral token
                    await WaitForInitializationCompletedAsync(new CancellationToken());

                    var userAgent = Constants.UserAgents.RandomElement();
                    var connectionParameters = new NodeConnectionParameters { UserAgent = userAgent };
                    var addrManTask = InitializeAddressManagerBehaviorAsync();
                    AddressManagerBehavior addressManagerBehavior = await addrManTask.ConfigureAwait(false);
                    connectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

                    if (TorManager?.State != TorState.Started && TorManager.State != TorState.Connected)
                    {
                        await TorManager.StartAsync(false, DataDir);
                    }

                    Nodes.Connect();
                    Logger.LogInfo("Global.OnResuming():Start connecting to nodes...");

                    var requestInterval = (Network == Network.RegTest) ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(30);
                    int maxFiltSyncCount = Network == Network.Main ? 1000 : 10000; // On testnet, filters are empty, so it's faster to query them together
                    Synchronizer.Resume(requestInterval, TimeSpan.FromMinutes(5), maxFiltSyncCount);
                    Logger.LogInfo("Global.OnResuming():Start synchronizing filters...");

                    if (WalletManager.SleepingCoins is { })
                    {
                        await WalletManager.CurrentWallet.ChaumianClient.QueueCoinsToMixAsync(WalletManager.SleepingCoins);
                        WalletManager.SleepingCoins = null;
                    }

                    IsResuming = false;
                    #endregion CriticalSection
                }
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogTrace($"Global.OnResuming(): Exception OperationCanceledException risen: {ex}");
            }
            finally
            {
                Logger.LogDebug("Global.OnResuming():Chaincase Resumed");
            }
        }

        public async Task OnSleeping()
        {
            if (IsGoingToSleep)
            {
                Logger.LogDebug($"Global.OnResuming(): ResumeCts.Cancel()");
                ResumeCts.Cancel();
                return;
            }

            try
            {
                Logger.LogDebug($"Global.OnSleeping(): Waiting for a lock");
                SleepCts.Dispose();
                SleepCts = new CancellationTokenSource();
                using (await LifeCycleMutex.LockAsync(SleepCts.Token))
                {
                    #region CriticalSection
                    Logger.LogDebug($"Global.OnSleeping(): Entered critical section");

                    IsGoingToSleep = true;

                    // don't ever cancel Init. use an ephemeral token
                    await WaitForInitializationCompletedAsync(new CancellationToken());


                    if (TorManager?.State != TorState.Stopped) // OnionBrowser && Dispose@Global
                    {
                        await TorManager.StopAsync();
                        Logger.LogInfo($"Global.OnSleeping():{nameof(TorManager)} is stopped.");
                    }

                    try
                    {
                        using var dequeueCts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
                        await WalletManager.DequeueAllCoinsGracefullyAsync(DequeueReason.ApplicationExit, dequeueCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error during {nameof(WalletManager.DequeueAllCoinsGracefullyAsync)}: {ex}");
                    }

                    var synchronizer = Synchronizer;
                    if (synchronizer is { })

                    {
                        await synchronizer.SleepAsync();
                        Logger.LogInfo($"Global.OnSleeping():{nameof(Synchronizer)} is sleeping.");
                    }

                    var addressManagerFilePath = AddressManagerFilePath;
                    if (addressManagerFilePath is { })
                    {
                        IoHelpers.EnsureContainingDirectoryExists(addressManagerFilePath);
                        var addressManager = AddressManager;
                        if (addressManager is { })
                        {
                            addressManager.SavePeerFile(AddressManagerFilePath, Config.Network);
                            Logger.LogInfo($"Global.OnSleeping():{nameof(AddressManager)} is saved to `{AddressManagerFilePath}`.");
                        }
                    }

                    var nodes = Nodes;
                    if (nodes is { })
                    {
                        nodes.Disconnect();
                        while (nodes.ConnectedNodes.Any(x => x.IsConnected))
                        {
                            await Task.Delay(50);
                        }

                        Logger.LogInfo($"Global.OnSleeping():{nameof(Nodes)} are disconnected.");
                    }

                    IsGoingToSleep = false;
                    #endregion CriticalSection
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Global.OnSleeping(): Error while sleeping: {ex}");
            }
            finally
            {
                Logger.LogDebug("Global.OnSleeping():Chaincase Sleeping"); // no real termination on iOS
            }
        }
    }
}
