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

namespace Wasabi.iOS.Tor
{
    public class TorProcessManager
    {
        /// <summary>
        /// If null then it's just a mock, clearnet is used.
        /// </summary>
        public EndPoint TorSocks5EndPoint { get; }

        public string LogFile { get; }

        public TORController TorController { get; private set; }

        private TORThread thread;

        public TorProcessManager(EndPoint torSocks5EndPoint, string logFile)
        {
            TorSocks5EndPoint = torSocks5EndPoint;
            LogFile = logFile;
            _running = 0;
            Stop = new CancellationTokenSource();
            TorController = null;
        }

        public static TorProcessManager Mock() // Mock, do not use Tor at all for debug.
        {
            return new TorProcessManager(null, null);
        }

        public void Start(bool ensureRunning, string dataDir)
        {
            if (TorSocks5EndPoint is null)
            {
                return;
            }
            if (TORThread.ActiveThread is null)
            {
                thread = new TORThread(Configuration);
                thread.Start();
            }
        }



        private TORConfiguration Configuration
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
                configuration.ControlSocket = NSUrl.CreateFileUrl(new string[] { homeDirectory, ".tor/control_port" });
                configuration.Arguments = new string[] { "--ignore-missing-torrc" };
                return configuration;
            }
        }

        static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;


        #region Monitor

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long _running;

        public bool IsRunning => Interlocked.Read(ref _running) == 1;

        private CancellationTokenSource Stop { get; set; }

        public void StartMonitor(TimeSpan torMisbehaviorCheckPeriod, TimeSpan checkIfRunningAfterTorMisbehavedFor, string dataDirToStartWith, Uri fallBackTestRequestUri)
        {
            if (TorSocks5EndPoint is null)
            {
                return;
            }

            Console.Write("Starting Tor monitor..."); // TODO Log nice
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
                                        // TODO pretty log
                                        Console.WriteLine($"Tor did not work properly for {(int)torMisbehavedFor.TotalSeconds} seconds. Maybe it crashed. Attempting to start it...");
                                        Start(true, dataDirToStartWith); // Try starting Tor, if it does not work it'll be another issue.
                                        await Task.Delay(14000, Stop.Token).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                        catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException || ex is TimeoutException)
                        {
                            Console.Write(ex); // TODO accessable log
                        }
                        catch (Exception ex)
                        {
                            Console.Write(ex); // TODO accessable log
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
            if(TorSocks5EndPoint is null)
            {
                Interlocked.Exchange(ref _running, 3);
            }

            Stop?.Cancel();
            while (Interlocked.CompareExchange(ref _running, 3, 0) == 2)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
            Stop?.Dispose();
            Stop = null;
            // --
            TorController?.Disconnect();
            TorController?.Dispose();
            TorController = null;

            thread?.Cancel();
            thread.Dispose();
            thread = null;
        }


        #endregion Monitor
    }
}
