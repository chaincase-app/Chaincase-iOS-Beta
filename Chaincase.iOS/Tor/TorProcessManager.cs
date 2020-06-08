using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using ObjCRuntime;
using TorFramework;
using WalletWasabi.Exceptions;
using WalletWasabi.Logging;
using WalletWasabi.TorSocks5;
using WalletWasabi.TorSocks5.Models.Fields.OctetFields;
using Xamarin.Forms;

[assembly: Dependency(typeof(Chaincase.iOS.Tor.OnionManager))]
namespace Chaincase.iOS.Tor
{
    public class OnionManager : ITorManager
    {
        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long _running;
        public TorState State;

        public OnionManager()
        {
            TorSocks5EndPoint = new IPEndPoint(IPAddress.Loopback, 9050);
            _running = 0;
            Stop = new CancellationTokenSource();
            TorController = null;
        }

        /// <summary>
        /// If null then it's just a mock, clearnet is used.
        /// </summary>
        public EndPoint TorSocks5EndPoint { get; }

        public string LogFile { get; }

        public static bool RequestFallbackAddressUsage { get; private set; } = false;

        public TORController TorController { get; private set; }

        private TORThread torThread;

        public bool IsRunning => Interlocked.Read(ref _running) == 1;

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
            // install geoip here
            if (TORThread.ActiveThread is null)
            {
                torThread = new TORThread(torBaseConf);
                torThread.Start();
                Logger.LogInfo($"Started Tor process with Tor.framework");
            }

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
        public void StartTor()
		{
            State = TorState.Started;

            // FIXME detect network change via reachability here

            if (torThread?.IsCancelled ?? true {

                torThread = null


            let torConf = OnionManager.torBaseConf


            var args = torConf.arguments!
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
                configuration.CookieAuthentication = new NSNumber(true); //@YES
                configuration.DataDirectory = new NSUrl(Path.GetTempPath());
                configuration.Arguments = new string[] {
                    "--allow-missing-torrc",
                    "--ignore-missing-torrc",
                    "--SocksPort", "localhost:9050"
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
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
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
                    Interlocked.CompareExchange(ref _running, 3, 2); // If IsStopping, make it stopped.
                }
            });
        }

        public async Task StopAsync()
        {
            Interlocked.CompareExchange(ref _running, 2, 1); // If running, make it stopping.

            if (TorSocks5EndPoint is null)
            {
                Interlocked.Exchange(ref _running, 3);
            }

            Stop?.Cancel();
            while (Interlocked.CompareExchange(ref _running, 3, 0) == 2)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
            // Under the hood, TORController will SIGNAL SHUTDOWN and set it's channel to nil, so
            // we actually rely on that to stop Tor and reset the state of torController. (we can
            // SIGNAL SHUTDOWN here, but we can't reset the torController "isConnected" state.)
            TorController?.Disconnect();
            TorController?.Dispose();
            TorController = null;

            torThread?.Cancel();
            torThread?.Dispose();
            torThread = null;
        }

        #endregion Monitor

        private static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;

    }
}
