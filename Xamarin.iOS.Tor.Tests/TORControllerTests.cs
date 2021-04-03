using Foundation;
using System;
using System.Threading;
using Xunit;
using Xamarin.iOS.Tor;
using System.Threading.Tasks;
using ObjCRuntime;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CoreFoundation;
using System.Diagnostics;

namespace Xamarin.iOS.Tor.Tests
{
    /* This is as close to a direct port as possible to the Tor.framework
     * tests written in objective-c. The goal of these tests is to ensure the
     * bindings have the same behavior as the original framework.
     * 
     * Minor changes in TORConfiguration reflect usage in Chaincase.iOS
     * e.g. --ControlPort
     * 
     */

    public delegate void TorLogCB(OSLogLevel severity, string msg);

    public class TORLogging
    {
        // extern void TORInstallEventLogging ();
        [DllImport("__Internal")]
        public static extern void TORInstallEventLogging();

        // extern void TORInstallEventLoggingCallback (tor_log_cb _Nonnull cb);
        [DllImport("__Internal")]
        public static extern void TORInstallEventLoggingCallback(TorLogCB cb);

        // extern void TORInstallTorLogging ();
        [DllImport("__Internal")]
        public static extern void TORInstallTorLogging();

        // extern void TORInstallTorLoggingCallback (tor_log_cb _Nonnull cb);
        [DllImport("__Internal")]
        public static extern void TORInstallTorLoggingCallback(TorLogCB cb);
    }

    public class TORControllerFixture
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
                    "--log", "notice stdout",
                    "--ignore-missing-torrc",
                    "--SocksPort", "127.0.0.1:9050",
                    "--ControlPort", "127.0.0.1:39060",
                };
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

        }
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
            this.Controller = new TORController("127.0.0.1", 39060);
            this.Cookie = NSData.FromUrl(fixture.Configuration.DataDirectory.Append("control_auth_cookie", false));
        }

        [Fact]
        public void CanLog()
        {
            TorLogCB cb = (severity, msg) => { Debug.WriteLine("YOLO"); };

            DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, TimeSpan.FromSeconds(1)), () =>
            {
                TORLogging.TORInstallTorLoggingCallback((severity, msg) =>
                {
                    string s;
                    switch (severity)
					{
                        case OSLogLevel.Debug:
                            s = "debug";
                            break;
                        case OSLogLevel.Error:
                            s = "error";
                            break;
                        case OSLogLevel.Fault:
                            s = "fault";
                            break;
                        case OSLogLevel.Info:
                            s = "info";
                            break;
						default:
                            s = "default";
                            break;
					}

                    Debug.WriteLine($"[Tor {s}] {msg.Trim()}");
                });
                TORLogging.TORInstallEventLoggingCallback((severity, msg) =>
                {
                    string s;
                    switch (severity)
                    {
                        case OSLogLevel.Debug:
                            // Ignore libevent debug messages. Just too many of typically no importance.
                            return;
                        case OSLogLevel.Error:
                            s = "error";
                            break;
                        case OSLogLevel.Fault:
                            s = "fault";
                            break;
                        case OSLogLevel.Info:
                            s = "info";
                            break;
                        default:
                            s = "default";
                            break;
                    }

                    Debug.WriteLine($"[libevent {s}] {msg}");
                });
            });
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

        // hack This, and all, timeouts will just succeed after 120 seconds. If we hang, it's really a failure.
        [Fact(Timeout = 120 * 1000)]
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

        // This will crash if it runs before the other tests
        // Could this be related to the main issue? Could closed or null circuits be referenced?
        [Fact(Timeout = 120 * 1000)]
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

        [Fact(Timeout = 120 * 1000)]
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
        private static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;


        [Fact]
        public void TestHiddenService()
        {
            CanLog();
            EventWaitHandle  ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            //System.Diagnostics.Debug.WriteLine(NSHomeDirectory());
            string serviceId = "";
            Exec(() =>
            {
                // ADD_ONION NEW:BEST Flags=DiscardPK Port=37129,37129"
                Controller.SendCommand(new NSString("ADD_ONION"), new string[] { "NEW:BEST" , "Port=37129,37129" , "Flags=DiscardPK"}, null,
                                   (keys, values, boolPointer) => {
//                                       Assert.True(keys[0] == (NSNumber)250);
                                       serviceId = values[0].ToString().Split('=')[1];

                                       ewh.Set();
                                       return true;
                                   });

            });
            ewh.WaitOne();

            Exec(() =>
			{

                Controller.SendCommand(new NSString("DEL_ONION"), new string[] { $"{serviceId}" }, null,
								   (keys, values, boolPointer) =>
								   {
                                       ewh.Set();

                                       return true;
								   });
			});
            ewh.WaitOne();
        }


        [Fact]
        public void CanDestroyHiddenService(string serviceId)
        {
            CanLog();
            EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            //System.Diagnostics.Debug.WriteLine(NSHomeDirectory());
            Exec(() =>
            {
                // ADD_ONION NEW:BEST Flags=DiscardPK Port=37129,37129"
                Controller.SendCommand(new NSString("DEL_ONION"), new string[] { $"SP {serviceId} CRLF" }, null,
                                   (keys, values, boolPointer) => {
                                       // Assert.True(values.)
                                       return true;
                                   });
                ewh.Set();
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
