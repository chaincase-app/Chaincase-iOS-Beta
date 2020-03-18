using System;
using System.IO;
using System.Linq;
using System.Net;
using Foundation;
using ObjCRuntime;
using TorFramework;
using Xamarin.Forms;

[assembly: Dependency(typeof(Chaincase.iOS.Tor.TorProcessManager))]
namespace Chaincase.iOS.Tor
{
    public class TorProcessManager : ITorManager
    {
        /// <summary>
        /// If null then it's just a mock, clearnet is used.
        /// </summary>
        public string TorSocks5EndPoint { get; }

        public string LogFile { get; }

        public TORController TorController { get; private set; }

        private TORThread thread;

        public TorProcessManager()
        {
            TorSocks5EndPoint = "Yolo?";
            TorController = null;
        }

        public ITorManager Mock() // Mock, do not use Tor at all for debug.
        {
            return new TorProcessManager();
        }

        public void Start(bool ensureRunning, string dataDir)
        {
            if (dataDir == "mock")
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

        public void Stop()
        {
            // Under the hood, TORController will SIGNAL SHUTDOWN and set it's channel to nil, so
            // we actually rely on that to stop Tor and reset the state of torController. (we can
            // SIGNAL SHUTDOWN here, but we can't reset the torController "isConnected" state.)
            TorController?.Disconnect();
            TorController?.Dispose();
            TorController = null;

            thread?.Cancel();
            thread?.Dispose();
            thread = null;
        }

        private static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;

    }
}
