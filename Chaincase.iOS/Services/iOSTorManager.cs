using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using CoreFoundation;
using Foundation;
using Nito.AsyncEx;
using ObjCRuntime;
using Xamarin.iOS.Tor;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.TorSocks5;

namespace Chaincase.iOS.Services
{
    public interface OnionManagerDelegate
    {
        void TorConnProgress(int progress);

        void TorConnFinished();

        void TorConnDifficulties();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "iOS is iOS")]
    public class iOSTorManager : BaseTorManager
    {
	    private NSData Cookie => NSData.FromUrl(torBaseConf.DataDirectory.Append("control_auth_cookie", false));
        public override TorState State { get; set; }

        public override async Task EnsureRunning()
        {
	        await Task.Delay(3000);
	        if (!await IsTorRunningAsync(TorSocks5EndPoint))
	        {
		        throw new TorException("Attempted to start Tor, but it is not running.");
	        }

	        Logger.LogInfo("TorProcessManager.StartAsync(): Tor is running.");

        }

        private DispatchBlock initRetry;

        public iOSTorManager(Global global, Config config) : base(global, config)
        {
	        TorSocks5EndPoint = new IPEndPoint(IPAddress.Loopback, 9050);
            TorController = null;
        }

        /// <summary>
        /// If null then it's just a mock, clearnet is used.
        /// </summary>
        public EndPoint TorSocks5EndPoint { get; }

        public string LogFile { get; }

        public static bool RequestFallbackAddressUsage { get; private set; } = false;

        private readonly AsyncLock mutex = new AsyncLock();

        public TORController TorController { get; private set; }

        private TORThread torThread;

        public override async Task StartAsyncCore(CancellationToken token)
        {
            if (TorSocks5EndPoint is null)
            {
                return;
            }
            await StartTor(null);
            Logger.LogInfo($"TorProcessManager.StartAsync(): Started Tor process with Tor.framework");

            
        }
        // port from iOS OnionBrowser


        public async Task StartTor(OnionManagerDelegate managerDelegate)
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

            if (torThread?.IsCancelled ?? true)
            {
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

            await Task.Delay(1000);
            // Wait long enough for Tor itself to have started. It's OK to wait for this
            // because Tor is already trying to connect; this is just the part that polls for
            // progress.

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

            var cookie = Guard.NotNull(nameof(Cookie), Cookie);
            TorController?.AuthenticateWithData(cookie, async (success, error) =>
            {
                using (await mutex.LockAsync())
                {
                    if (success)
                    {
                        NSObject completeObs = null;
                        completeObs = TorController?.AddObserverForCircuitEstablished((established) =>
                        {
                            if (established)
                            {
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
                }
            });

            initRetry = new DispatchBlock(async () =>
           {
               using (await mutex.LockAsync())
               {
                   Logger.LogDebug("Triggering Tor connection retry.");

                   TorController?.SetConfForKey("DisableNetwork", "1", null);
                   TorController?.SetConfForKey("DisableNetwork", "0", null);

                    // Hint user that they might need to use a bridge.
                    managerDelegate?.TorConnDifficulties();
               }
           });

            // On first load: If Tor hasn't finished bootstrap in 30 seconds,
            // HUP tor once in case we have partially bootstrapped but got stuck.
            DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, TimeSpan.FromSeconds(15)), initRetry!);
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

        public override async Task StopAsyncCore(CancellationToken cancellationToken)
        {
	        Logger.LogDebug($"TorProcessManager.StopAsync(): start stopAsync");
	        using (await mutex.LockAsync())
	        {
		        try
		        {
			        // Under the hood, TORController.Disconnect() will SIGNAL SHUTDOWN and set it's channel to null, so
			        // we actually rely on that to stop Tor and reset the state of torController. (we can
			        // SIGNAL SHUTDOWN here, but we can't reset the torController "isConnected" state.)
			        TorController?.Disconnect();
			        TorController?.Dispose();
			        TorController = null;

			        torThread?.Cancel();
			        torThread?.Dispose();
			        torThread = null;

			        State = TorState.Stopped;
		        }
		        catch (Exception error)
		        {
			        Logger.LogError($"TorProcessManager.StopAsync(): Failed to stop tor thread {error}");
		        }
	        }
        }

        private static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;

        // Cancel the connection retry and fail guard.
        private void CancelInitRetry()
        {
            initRetry?.Cancel();
            initRetry = null;
        }
    }
}
