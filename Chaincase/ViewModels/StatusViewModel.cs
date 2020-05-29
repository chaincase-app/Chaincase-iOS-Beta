using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chaincase.Models;
using Chaincase.Navigation;
using NBitcoin.Protocol;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Blocks;
using WalletWasabi.Logging;
using WalletWasabi.Models;
using WalletWasabi.Services;
using WalletWasabi.Wallets;
using Xamarin.Forms;

namespace Chaincase.ViewModels
{
    public class StatusViewModel : ViewModelBase
    {
        protected Global Global { get; }

        private CompositeDisposable Disposables { get; } = new CompositeDisposable();
        private NodesCollection Nodes { get; set; }
        private WasabiSynchronizer Synchronizer { get; set; }
        private SmartHeaderChain HashChain { get; set; }

        private ObservableAsPropertyHelper<double> _progressPercent;

        private bool UseTor { get; set; }

        private UpdateStatus _updateStatus;
        private bool _updateAvailable;
        private bool _criticalUpdateAvailable;

        private BackendStatus _backend;
        private TorStatus _tor;
        private int _peers;
        private int _maxFilters = -1;
        private ObservableAsPropertyHelper<int> _filtersLeft;
        private string _btcPrice;
        private ObservableAsPropertyHelper<string> _status;
        private bool _downloadingBlock;
        private StatusSet ActiveStatuses { get; }


        public StatusViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
        {
            Global = Locator.Current.GetService<Global>();
            Backend = BackendStatus.NotConnected;
            UseTor = false;
            Tor = TorStatus.NotRunning;
            Peers = 0;
            BtcPrice = "$0";
            ActiveStatuses = new StatusSet();

            UseTor = Global.Config.UseTor; // Do not make it dynamic, because if you change this config settings only next time will it activate.

            _status = ActiveStatuses.WhenAnyValue(x => x.CurrentStatus)
                .Select(x => x.ToString())
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.Status)
                .DisposeWith(Disposables);

            _progressPercent = ActiveStatuses.WhenAnyValue(x => x.CurrentStatus)
                .Select(status =>
                {
                    switch (status.Type)
                    {
                        case StatusType.Ready:
                            return 1;
                        case StatusType.Synchronizing:
                            return status.Percentage / 200.0 + 0.3;
                        case StatusType.Connecting:
                        default:
                            return 0.3;

                    }
                })
                .ToProperty(this, x => x.ProgressPercent);

            Task.Run(async () =>
            {
                // WaitForInitializationCompletedAsync could tune to wait for Global.[Nodes.ConnectedNodes/Synchronizer] init via Rx
                using var initCts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
                await Global.WaitForInitializationCompletedAsync(initCts.Token).ConfigureAwait(false);
                var nodes = Global.Nodes.ConnectedNodes;
                var synchronizer = Global.Synchronizer;
                Device.BeginInvokeOnMainThread(() =>
                {
                    Nodes = nodes;
                    Synchronizer = synchronizer;
                    HashChain = synchronizer.BitcoinStore.SmartHeaderChain;

                    Observable
                        .Merge(Observable.FromEventPattern<NodeEventArgs>(nodes, nameof(nodes.Added)).Select(x => true)
                        .Merge(Observable.FromEventPattern<NodeEventArgs>(nodes, nameof(nodes.Removed)).Select(x => true)
                        .Merge(Synchronizer.WhenAnyValue(x => x.TorStatus).Select(x => true))))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => Peers = Synchronizer.TorStatus == TorStatus.NotRunning ? 0 : Nodes.Count) // Set peers to 0 if Tor is not running, because we get Tor status from backend answer so it seems to the user that peers are connected over clearnet, while they are not.
                        .DisposeWith(Disposables);

                    Synchronizer.WhenAnyValue(x => x.TorStatus)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(status => Tor = UseTor ? status : TorStatus.TurnedOff)
                        .DisposeWith(Disposables);

                    Synchronizer.WhenAnyValue(x => x.BackendStatus)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => Backend = Synchronizer.BackendStatus)
                        .DisposeWith(Disposables);

                    _filtersLeft = HashChain.WhenAnyValue(x => x.HashesLeft)
                        .Throttle(TimeSpan.FromMilliseconds(100))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .ToProperty(this, x => x.FiltersLeft)
                        .DisposeWith(Disposables);

                    Synchronizer.WhenAnyValue(x => x.UsdExchangeRate)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(usd => BtcPrice = $"${(long)usd}")
                        .DisposeWith(Disposables);
                });
            });

            Peers = Tor == TorStatus.NotRunning ? 0 : Nodes.Count;

            Observable.FromEventPattern<bool>(typeof(Wallet), nameof(Wallet.DownloadingBlockChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => DownloadingBlock = x.EventArgs)
                .DisposeWith(Disposables);

            IDisposable walletCheckingInterval = null;
            Observable.FromEventPattern<bool>(typeof(Wallet), nameof(Wallet.InitializingChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (x.EventArgs)
                    {
                        TryAddStatus(StatusType.WalletLoading);

                        if (walletCheckingInterval is null)
                        {
                            walletCheckingInterval = Observable.Interval(TimeSpan.FromSeconds(1))
                               .ObserveOn(RxApp.MainThreadScheduler)
                               .Subscribe(_ =>
                               {
                                   var wallet = Global.Wallet;
                                   if (wallet is { })
                                   {
                                       var segwitActivationHeight = SmartHeader.GetStartingHeader(wallet.Network).Height;
                                       if (wallet.LastProcessedFilter?.Header?.Height is uint lastProcessedFilterHeight
                                            && lastProcessedFilterHeight > segwitActivationHeight
                                            && Global.BitcoinStore?.SmartHeaderChain?.TipHeight is uint tipHeight
                                            && tipHeight > segwitActivationHeight)
                                       {
                                           var allFilters = tipHeight - segwitActivationHeight;
                                           var processedFilters = lastProcessedFilterHeight - segwitActivationHeight;
                                           var perc = allFilters == 0 ?
                                                100
                                                : ((decimal)processedFilters / allFilters * 100);
                                           TryAddStatus(StatusType.WalletProcessingFilters, (ushort)perc);
                                       }

                                       var txProcessor = wallet.TransactionProcessor;
                                       if (txProcessor is { })
                                       {
                                           var perc = txProcessor.QueuedTxCount == 0 ?
                                                100
                                                : ((decimal)txProcessor.QueuedProcessedTxCount / txProcessor.QueuedTxCount * 100);
                                           TryAddStatus(StatusType.WalletProcessingTransactions, (ushort)perc);
                                       }
                                   }
                               }).DisposeWith(Disposables);
                        }
                    }
                    else
                    {
                        walletCheckingInterval?.Dispose();
                        walletCheckingInterval = null;
                        Global.UiConfig.Balance = Global.Wallet.Coins.TotalAmount().ToString();
                        Global.UiConfig.ToFile();
                        TryRemoveStatus(StatusType.WalletLoading, StatusType.WalletProcessingFilters, StatusType.WalletProcessingTransactions);
                    }
                })
                .DisposeWith(Disposables);

            this.WhenAnyValue(x => x.FiltersLeft, x => x.DownloadingBlock)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tup =>
                {
                    (int filtersLeft, bool downloadingBlock) = tup;
                    if (filtersLeft == 0 && !downloadingBlock)
                    {
                        TryRemoveStatus(StatusType.Synchronizing);
                    }
                    else
                    {
                        ushort perc = 0;
                        if (filtersLeft > 0)
						{
                            if (_maxFilters == -1)
							{
                                _maxFilters = filtersLeft;
                            }
                            perc =  Convert.ToUInt16((double)(_maxFilters - filtersLeft) / filtersLeft * 100);
                        }

                        TryAddStatus(StatusType.Synchronizing, perc);
                    }
                });

            this.WhenAnyValue(x => x.Tor, x => x.Backend, x => x.Peers)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tup =>
                {
                    (TorStatus tor, BackendStatus backend, int peers) = tup;

                    // The source of the p2p connection comes from if we use Core for it or the network.
                    var p2pConnected = peers >= 1;
                    if (tor == TorStatus.NotRunning || backend != BackendStatus.Connected || !p2pConnected)
                    {
                        TryAddStatus(StatusType.Connecting);
                    }
                    else
                    {
                        TryRemoveStatus(StatusType.Connecting);
                    }
                });
        }

        public BackendStatus Backend
        {
            get => _backend;
            set => this.RaiseAndSetIfChanged(ref _backend, value);
        }

        public TorStatus Tor
        {
            get => _tor;
            set => this.RaiseAndSetIfChanged(ref _tor, value);
        }

        public int Peers
        {
            get => _peers;
            set => this.RaiseAndSetIfChanged(ref _peers, value);
        }

        public double ProgressPercent => _progressPercent?.Value ?? 0.4;

        public int FiltersLeft => _filtersLeft?.Value ?? 0;

        public UpdateStatus UpdateStatus
        {
            get => _updateStatus;
            set => this.RaiseAndSetIfChanged(ref _updateStatus, value);
        }

        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set => this.RaiseAndSetIfChanged(ref _updateAvailable, value);
        }

        public bool CriticalUpdateAvailable
        {
            get => _criticalUpdateAvailable;
            set => this.RaiseAndSetIfChanged(ref _criticalUpdateAvailable, value);
        }

        public string BtcPrice
        {
            get => _btcPrice;
            set => this.RaiseAndSetIfChanged(ref _btcPrice, value);
        }

        public string Status => _status?.Value ?? "Loading...";

        public bool DownloadingBlock
        {
            get => _downloadingBlock;
            set => this.RaiseAndSetIfChanged(ref _downloadingBlock, value);
        }

        public void TryAddStatus(StatusType status, ushort percentage = 0)
        {
            try
            {
                ActiveStatuses.Set(new Status(status, percentage));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex);
            }
        }

        public void TryRemoveStatus(params StatusType[] statuses)
        {
            try
            {
                ActiveStatuses.Complete(statuses);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex);
            }
        }

        #region IDisposable Support

        private volatile bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Disposables?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}
