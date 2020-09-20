using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using ObjCRuntime;
using TorFramework;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.TorSocks5;
using WalletWasabi.TorSocks5.Models.Fields.OctetFields;
using Xamarin.Forms;

[assembly: Dependency(typeof(Chaincase.iOS.Tor.OnionManager))]
namespace Chaincase.iOS.Tor
{
    public interface OnionManagerDelegate
    {
        void TorConnProgress(int progress);

        void TorConnFinished();

        void TorConnDifficulties();
    }

    public class OnionManager : ITorManager
    {
        private const long StateNotStarted = 0;

        private const long StateRunning = 1;

        private const long StateStopping = 2;

        private const long StateStopped = 3;

        /// <summary>
        /// Value can be any of: <see cref="StateNotStarted"/>, <see cref="StateRunning"/>, <see cref="StateStopping"/> and <see cref="StateStopped"/>.
        /// </summary>
        private long _monitorState;
        private NSData Cookie; 
        public TorState State { get; set; }
        private DispatchBlock initRetry;

        public OnionManager()
        {
            TorSocks5EndPoint = new IPEndPoint(IPAddress.Loopback, 9050);
            _monitorState = StateNotStarted;
            Stop = new CancellationTokenSource();
            TorController = null;
            Cookie = NSData.FromUrl(torBaseConf.DataDirectory.Append("control_auth_cookie", false));
        }

        /// <summary>
        /// If null then it's just a mock, clearnet is used.
        /// </summary>
        public EndPoint TorSocks5EndPoint { get; }

        public string LogFile { get; }

        public static bool RequestFallbackAddressUsage { get; private set; } = false;

        public TORController TorController { get; private set; }

        private TORThread torThread;

        public bool IsRunning => Interlocked.Read(ref _monitorState) == StateRunning;

        private CancellationTokenSource Stop { get; set; }

        public ITorManager Mock() // Mock, do not use Tor at all for debug.
        {
            return new OnionManager();
        }

		public void Start(bool ensureRunning, string dataDir)
		{
			if (TorSocks5EndPoint is null)
			{
				return;
			}

			if (dataDir == "mock")
			{
				return;
			}
			
            StartTor(null);
			Logger.LogInfo($"Started Tor process with Tor.framework");

			if (ensureRunning)
			{
				Task.Delay(3000).ConfigureAwait(false).GetAwaiter().GetResult(); // dotnet brainfart, ConfigureAwait(false) IS NEEDED HERE otherwise (only on) Manjuro Linux fails, WTF?!!
				if (!IsTorRunningAsync(TorSocks5EndPoint).GetAwaiter().GetResult())
				{
					throw new TorException("Attempted to start Tor, but it is not running.");
				}
				Logger.LogInfo("Tor is running.");
			}
		}

		// port from iOS OnionBrowser
		public void StartTor(OnionManagerDelegate managerDelegate)
        {
            // In objective-c this is weak to avoid retain cycle. not sure if we
            // need to replicate cause we got a GC
            var weakDelegate = managerDelegate;

            CancelInitRetry();
            State = TorState.Started;

            if (TorController == null)
            {
                TorController = new TORController("127.0.0.1", 39060);
            }

            if (torThread?.IsCancelled ?? true) {
                torThread?.Dispose();
                torThread = null;

                var torConf = this.torBaseConf;

                var args = torConf.Arguments;

                Logger.LogDebug(String.Join(" ", args));

                torConf.Arguments = args;

                torThread = new TORThread(torConf);

                torThread?.Start();

                Logger.LogInfo("Starting Tor");
            }

            // Wait long enough for Tor itself to have started. It's OK to wait for this
            // because Tor is already trying to connect; this is just the part that polls for
            // progress.
            DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, TimeSpan.FromSeconds(1)), () =>
            {
                if (!(TorController?.Connected ?? false))
                {
                    // do
                    NSError e = null;
                    TorController?.Connect(out e);
                    if (e != null) // faux catch accomodates bound obj-c)
                    {
                        Logger.LogError(e.LocalizedDescription);
                    }
                }

                //var cookie = Guard.NotNull(nameof(Cookie), Cookie);
                var cookie = NSData.FromUrl(torBaseConf.DataDirectory.Append("control_auth_cookie", false));

                TorController?.AuthenticateWithData(cookie, (success, error) =>
                {
                    if (success)
                    {
                        NSObject completeObs = null;
                        completeObs = TorController.AddObserverForCircuitEstablished((established) =>
                        {
                            if (established) {
                                State = TorState.Connected;

                                TorController?.RemoveObserver(completeObs);
                                CancelInitRetry();
                                Logger.LogDebug("Connection established!");

                                weakDelegate?.TorConnFinished();
                            }
                        }); // TorController.addObserver

                        NSObject progressObs = null;
                        progressObs = TorController?.AddObserverForStatusEvents(
                            (NSString type, NSString severity, NSString action, NSDictionary<NSString, NSString> arguments) =>
                            {
                                if (type == "STATUS_CLIENT" && action == "BOOTSTRAP")
                                {
                                    var progress = Int32.Parse(arguments![(NSString)"PROGRESS"]!)!;
                                    Logger.LogDebug(progress.ToString());

                                    weakDelegate?.TorConnProgress(progress);

                                    if (progress >= 100)
                                    {
                                        TorController?.RemoveObserver(progressObs);
                                    }

                                    return true;
                                }

                                return false;
                            }); // TorController.addObserver
                    } // if success (authenticate)
                    else
					{
                        Logger.LogInfo("Didn't connect to control port.");
					}
                });
            }); //delay

            initRetry = new DispatchBlock(() => {
                Logger.LogDebug("Triggering Tor connection retry.");

                TorController?.SetConfForKey("DisableNetwork", "1", null);
                TorController?.SetConfForKey("DisableNetwork", "0", null);

                // Hint user that they might need to use a bridge.
                managerDelegate?.TorConnDifficulties();
            });

            // On first load: If Tor hasn't finished bootstrap in 30 seconds,
            // HUP tor once in case we have partially bootstrapped but got stuck.
            DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, TimeSpan.FromSeconds(15)), initRetry);
        }

        private TORConfiguration torBaseConf
        {
            get
            {
                string homeDirectory = null;

                if (Runtime.Arch == Arch.SIMULATOR)
                {
                    foreach (string var in new string[] { "IPHONE_SIMULATOR_HOST_HOME", "SIMULATOR_HOST_HOME" })
                    {
                        string val = Environment.GetEnvironmentVariable(var);
                        if (val != null)
                        {
                            homeDirectory = val;
                            break;
                        }
                    }
                }
                else
                {
                    homeDirectory = NSHomeDirectory();
                }

                TORConfiguration configuration = new TORConfiguration();
                configuration.CookieAuthentication = true; //@YES
                configuration.DataDirectory = new NSUrl(Path.GetTempPath());
                configuration.Arguments = new string[] {
                    "--allow-missing-torrc",
                    "--ignore-missing-torrc",
                    "--SocksPort", "127.0.0.1:9050",
                    "--ControlPort", "127.0.0.1:39060",
                };
                return configuration;
            }
        }

        /// <param name="torSocks5EndPoint">Opt out Tor with null.</param>
        public static async Task<bool> IsTorRunningAsync(EndPoint torSocks5EndPoint)
        {
            using var client = new TorSocks5Client(torSocks5EndPoint);
            try
            {
                await client.ConnectAsync().ConfigureAwait(false);
                await client.HandshakeAsync().ConfigureAwait(false);
            }
            catch (ConnectionException)
            {
                return false;
            }
            return true;
        }

        #region Monitor

        public void StartMonitor(TimeSpan torMisbehaviorCheckPeriod, TimeSpan checkIfRunningAfterTorMisbehavedFor, string dataDirToStartWith, Uri fallBackTestRequestUri)
        {
            if (TorSocks5EndPoint is null)
            {
                return;
            }

            Logger.LogInfo("Starting Tor monitor...");
            if (Interlocked.CompareExchange(ref _monitorState, StateRunning, StateNotStarted) != StateNotStarted)
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    while (IsRunning)
                    {
                        try
                        {
                            await Task.Delay(torMisbehaviorCheckPeriod, Stop.Token).ConfigureAwait(false);

                            if (TorHttpClient.TorDoesntWorkSince != null) // If Tor misbehaves.
                            {
                                TimeSpan torMisbehavedFor = (DateTimeOffset.UtcNow - TorHttpClient.TorDoesntWorkSince) ?? TimeSpan.Zero;

                                if (torMisbehavedFor > checkIfRunningAfterTorMisbehavedFor)
                                {
                                    if (TorHttpClient.LatestTorException is TorSocks5FailureResponseException torEx)
                                    {
                                        if (torEx.RepField == RepField.HostUnreachable)
                                        {
                                            Uri baseUri = new Uri($"{fallBackTestRequestUri.Scheme}://{fallBackTestRequestUri.DnsSafeHost}");
                                            using (var client = new TorHttpClient(baseUri, TorSocks5EndPoint))
                                            {
                                                var message = new HttpRequestMessage(HttpMethod.Get, fallBackTestRequestUri);
                                                await client.SendAsync(message, Stop.Token).ConfigureAwait(false);
                                            }

                                            // Check if it changed in the meantime...
                                            if (TorHttpClient.LatestTorException is TorSocks5FailureResponseException torEx2 && torEx2.RepField == RepField.HostUnreachable)
                                            {
                                                // Fallback here...
                                                RequestFallbackAddressUsage = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Logger.LogInfo($"Tor did not work properly for {(int)torMisbehavedFor.TotalSeconds} seconds. Maybe it crashed. Attempting to start it...");
                                        Start(true, dataDirToStartWith); // Try starting Tor, if it does not work it'll be another issue.
                                        await Task.Delay(14000, Stop.Token).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                        catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException || ex is TimeoutException)
                        {
                            Logger.LogTrace(ex);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug(ex);
                        }
                    }
                }
                finally
                {
                    Interlocked.CompareExchange(ref _monitorState, StateStopped, StateStopping); // If IsStopping, make it stopped.
                }
            });
        }

        public async Task StopAsync()
        {
            Interlocked.CompareExchange(ref _monitorState, StateStopping, StateRunning); // If running, make it stopping.   

            if (TorSocks5EndPoint is null)
            {
                Interlocked.Exchange(ref _monitorState, StateStopped);
            }

            Stop?.Cancel();

            // Under the hood, TORController will SIGNAL SHUTDOWN and set it's channel to nil, so
            // we actually rely on that to stop Tor and reset the state of torController. (we can
            // SIGNAL SHUTDOWN here, but we can't reset the torController "isConnected" state.)
            TorController?.Disconnect();
            TorController?.Dispose();
            TorController = null;

            torThread?.Cancel();
            torThread?.Dispose();
            torThread = null;

            State = TorState.Stopped;

            while (Interlocked.CompareExchange(ref _monitorState, StateStopped, StateNotStarted) == StateStopping)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        #endregion Monitor

        private static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;

        // Cancel the connection retry and fail guard.
        private void CancelInitRetry()
        {
            initRetry?.Cancel();
            initRetry = null;
        }
    }
}
