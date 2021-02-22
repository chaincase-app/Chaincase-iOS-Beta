using Foundation;
using System;
using System.Threading;
using Xunit;
using Xamarin.iOS.Tor;
using System.Threading.Tasks;
using ObjCRuntime;
using System.IO;
using System.Linq;

namespace Xamarin.iOS.Tor.Tests
{
    public class RunStopConnectFixture
    {
        public TORConfiguration Configuration
        {
            get
            {
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

        public TORThread thread;

        public RunStopConnectFixture()
        {
            // TORThread won't stop til the process does
            if (TORThread.ActiveThread is null)
            {
                thread = new TORThread(Configuration);
                thread.Start();
            }

            NSRunLoop.Main.RunUntil(NSDate.FromTimeIntervalSinceNow(0.5f));
        }
    }

    [CollectionDefinition("RunStopConnectCollection", DisableParallelization = true)]
    public class RunStopConnectCollection : ICollectionFixture<RunStopConnectFixture>
    {
        // No code, just definition
    }

    [Collection("RunStopConnectCollection")]
    public class RunStopConnectTests : IClassFixture<RunStopConnectFixture>
    {
        RunStopConnectFixture fixture;
        public TORController Controller;
        public NSData Cookie;


        public RunStopConnectTests(RunStopConnectFixture fixture)
        {
            this.fixture = fixture;
            this.Controller = new TORController("127.0.0.1", 39060);
            this.Cookie = NSData.FromUrl(fixture.Configuration.DataDirectory.Append("control_auth_cookie", false));
        }

        [Fact]
        public void TestRunStopConnectShouldCrash()
        {
            Controller.Connect(out NSError e);

            Controller.Disconnect();
            Controller.Dispose();
            Controller = null;

            fixture.thread?.Cancel();
            fixture.thread?.Dispose();
            fixture.thread = null;

            Controller.Connect(out e);
            Assert.True(e != null);
        }
    }
}
