using System;
using Xunit;
using TorFramework;
using System.Threading.Tasks;
using ObjCRuntime;
using Foundation;
using System.IO;
using System.Linq;
using System.Threading;

namespace TorFramework.Tests
{
    public class TORControllerFixture
    {
        public TORConfiguration Configuration
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

        private TORThread thread;

        public TORControllerFixture()
        {
            // TORThread won't stop til the process does
            if (TORThread.ActiveThread is null)
            {
                thread = new TORThread(Configuration);
                thread.Start();
            }

            NSRunLoop.Main.RunUntil(NSDate.FromTimeIntervalSinceNow(0.5f));
        }

        static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;
    }

    [CollectionDefinition("TORControllerCollection", DisableParallelization = true)]
    public class TORControllerCollection : ICollectionFixture<TORControllerFixture>
    {
        // No code, just definition
    }

    [Collection("TORControllerCollection")]
    public class ControllerTests : IClassFixture<TORControllerFixture>
    {
        TORControllerFixture fixture;
        public TORController Controller;
        public NSData Cookie;


        public ControllerTests(TORControllerFixture fixture)
        {
            this.fixture = fixture;
            this.Controller = new TORController(fixture.Configuration.ControlSocket);
            this.Cookie = NSData.FromUrl(fixture.Configuration.DataDirectory.Append("control_auth_cookie", false));
        }

        [Fact]
        public async Task TestCookieAutheniticationFailure()
        {
            var (success, error) = await Controller.AuthenticateWithDataAsync(new NSString("invalid").Encode(NSStringEncoding.UTF8));
            Assert.False(success);
            Assert.True(string.Equals(error.Domain, Constants.TORControllerErrorDomain.ToString()));
            Assert.True(error.Code != new nint(250));
        }

        [Fact]
        public async Task TestCookieAuthenticationSuccess()
        {
            var (success, error) = await Controller.AuthenticateWithDataAsync(Cookie);
            Assert.True(success);
            Assert.Null(error);
        }

        [Fact(Timeout = 2 * 1000)]
        public void TestSessionConfiguration()
        {
            EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            Exec(() =>
            {
                Controller.GetSessionConfiguration((NSUrlSessionConfiguration configuration) =>
                {
                    NSUrlSession session = NSUrlSession.FromConfiguration(configuration);
                    session.CreateDataTask(NSUrl.FromString("https://facebookcorewwwi.onion/"), (NSData data, NSUrlResponse response, NSError error) =>
                    {
                        Assert.Equal(((NSHttpUrlResponse)response).StatusCode, 200);
                        Assert.True(error is null);
                        ewh.Set();
                    }).Resume();
                });
            });

            ewh.WaitOne();
        }

        [Fact(Timeout = 2 * 1000)]
        public void TestGetAndCloseCircuits()
        {
            EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            Exec(() =>
            {
                Controller.GetCircuits((NSArray<TORCircuit> circuits) =>
                {
                    Console.WriteLine("circuits={0}", circuits);

                    foreach (TORCircuit circuit in circuits)
                    {
                        foreach (TORNode node in circuit.Nodes)
                        {
                            Assert.True(node.Fingerprint.Length > 0, "A circuit should have a fingerprint.");
                            Assert.True(node.Ipv4Address.Length > 0 || node.Ipv6Address.Length > 0, "A circuit should have an IPv4 or IPv6 address.");
                        }
                    }

                    Controller.CloseCircuits(circuits.ToArray(), (bool success) =>
                    {
                        Assert.True(success, "Circuits were closed successfully");

                        ewh.Set();
                    });
                });
            });

            ewh.WaitOne();
        }

        [Fact(Timeout = 2 * 1000)]
        public void TestReset()
        {
            EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            Exec(() =>
            {
                Controller.ResetConnection((bool success) =>
                {
                    Console.WriteLine("success={0}", success);

                    Assert.True(success, "Reset should work correctly");

                    ewh.Set();
                });
            });

            ewh.WaitOne();
        }

        public delegate void Callback();

        public void Exec(Callback callback)
        {
            TORController controller = this.Controller;

            var (success, error) = controller.AuthenticateWithDataAsync(Cookie).Result;
            Assert.True(success);
            Assert.Null(error);

            // This can't be async'd
            controller.AddObserverForCircuitEstablished((bool established) =>
            {
                if (!established)
                {
                    return;
                }
                callback();
            });
        }
    }
}
