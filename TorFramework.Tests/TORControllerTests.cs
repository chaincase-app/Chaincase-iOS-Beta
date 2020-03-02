
using Foundation;
using System;
using NUnit.Framework;
using System.Runtime.InteropServices;
using System.Threading;
using NUnitLite;
using TorFramework;
using System.IO;
using System.Linq;
using ObjCRuntime;
using System.Threading.Tasks;
using System.Threading;

namespace TorFramework.Tests
{

    interface ITORControllerTests
    {
        TORController Controller { get; set; }// @property (nonatomic, strong) TORController *controller;
        NSData Cookie { get; } // @property (readonly) NSData *cookie;
    }


	[TestFixture]
	public class TORControllerTests : ITORControllerTests
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

        [TestFixtureSetUp]
		protected void SetUpOnce()
		{
            TORThread thread = new TORThread(Configuration);
			thread.Start();

			NSRunLoop.Main.RunUntil(NSDate.FromTimeIntervalSinceNow(0.5f));
        }

        public TORController Controller { get; set; }
        [SetUp]
        public void SetUp()
        {
            Controller = new TORController(Configuration.ControlSocket);
        }

        [Test]
        public async Task TestCookieAutheniticationFailure()
        {
            var (success, error) = await Controller.AuthenticateWithDataAsync(new NSString("invalid").Encode(NSStringEncoding.UTF8));
            Assert.False(success);
            Assert.True(string.Equals(error.Domain, Constants.TORControllerErrorDomain.ToString()));
            Assert.True(error.Code != new nint(250));
        }

        [Test]
        public async Task TestCookieAuthenticationSuccess()
        {
            NSUrl cookieUrl = Configuration.DataDirectory.Append("control_auth_cookie", false);
            NSData cookie = NSData.FromUrl(cookieUrl);
            var (success, error) = await Controller.AuthenticateWithDataAsync(cookie);
            Assert.True(success);
            Assert.Null(error);
        }


        //[Test]
        //public async Task TestSessionConfiguration()
        //{
        //    await Exec(async () =>
        //    {
        //        NSUrlSessionConfiguration configuration = await Controller.GetSessionConfigurationAsync();
        //        NSUrlSession session = NSUrlSession.FromConfiguration(configuration);
        //        session.CreateDataTask(NSUrl.FromString("https://facebookcorewwwi.onion/"), (NSData data, NSUrlResponse response, NSError error) =>
        //        {
        //            Assert.Equals(((NSHttpUrlResponse)response).StatusCode, 200);
        //            Assert.True(error is null);
        //        }).Resume();
        //    });
        //}

        //[Test]
        //public void TestSessionConfiguration()
        //{
        //    EventWaitHandle done = new EventWaitHandle(false, EventResetMode.AutoReset);
        //    NSHttpUrlResponse res = null;
        //    NSError err = null;
        //    Exec(delegate()
        //    {
        //    //NSUrlSessionConfiguration configuration = Controller.GetSessionConfigurationAsync().Result;
        //    NSUrlSessionConfiguration config = NSUrlSessionConfiguration.DefaultSessionConfiguration;
        //    var keys = new NSObject[]
        //    {
        //        NSObject.FromObject("kCFProxyTypeKey"),
        //        NSObject.FromObject("kCFStreamPropertySOCKSProxyHost"),
        //        NSObject.FromObject("kCFStreamPropertySOCKSProxyPort")
        //    };
        //    var vals = new NSObject[]
        //    {
        //        NSObject.FromObject("kCFProxyTypeSOCKS"),
        //        new NSString("localhost"),
        //        NSNumber.FromInt32(9050)
        //    };
        //    config.ConnectionProxyDictionary = new NSDictionary<NSObject, NSObject>(keys, vals);
        //    NSUrlSession session = NSUrlSession.FromConfiguration(config);
        //        //session.CreateDataTask(NSUrl.FromString("https://check.torproject.org/"), (NSData data, NSUrlResponse response, NSError error) =>
        //TODO Decode gzip from check.torproject.org to see if running TOR or not
        //        session.CreateDataTask(NSUrl.FromString("https://www.facebookcorewwwi.onion"), (NSData data, NSUrlResponse response, NSError error) =>
        //        {
        //            res = (NSHttpUrlResponse)response;
        //            err = error;
        //            done.Set();
        //        }).Resume();
        //    });
        //    done.WaitOne();
        //    Assert.True(res.StatusCode == 200);
        //    Assert.True(err is null);
        //}

        //- (void) testGetAndCloseCircuits
        //        {
        //            XCTestExpectation *expectation = [self expectationWithDescription:@"resolution callback"];

        //            [self exec:^{
        //        [self.controller getCircuits:^(NSArray<TORCircuit*>* _Nonnull circuits) {
        //            NSLog(@"circuits=%@", circuits);

        //            for (TORCircuit* circuit in circuits)
        //            {
        //                for (TORNode* node in circuit.nodes) {
        //                    XCTAssert(node.fingerprint.length > 0, @"A circuit should have a fingerprint.");
        //        XCTAssert(node.ipv4Address.length > 0 || node.ipv6Address.length > 0, @"A circuit should have an IPv4 or IPv6 address.");
        //    }
        //}

        //[self.controller closeCircuits:circuits completion:^(BOOL success) {

        //    XCTAssertTrue(success, @"Circuits were closed successfully.");

        //                [expectation fulfill];
        //            }];
        //        }];
        //    }];

        //    [self waitForExpectationsWithTimeout:120 handler:nil];
        //}
        //[Test]
        //public async void TestGetAndCloseCircuits()
        //{
        //    var autoEvent = new AutoResetEvent(false); // initialize to false
        //    TORController controller = this.Controller;

        //    var (success, error) = await controller.AuthenticateWithDataAsync(Cookie);
        //    Assert.True(success);
        //    Assert.Null(error);

        //    var j = controller.AddObserverForCircuitEstablished(async (established) => await ToEstablish(established, autoEvent));
        //    autoEvent.WaitOne(); // wait until event set
        //}

        //public async Task ToEstablish(bool established, AutoResetEvent autoEvent)
        //{
        //    //SynchronizationContext.SetSynchronizationContext(new NUnit.Framework.AsyncSynchronizationContext);
        //    if (!established)
        //    {
        //        return;
        //    }
        //    NSArray<TORCircuit> circuits = await Controller.GetCircuitsAsync();
        //    Console.WriteLine("circuits=" + circuits);
        //    foreach (var circuit in circuits)
        //    {
        //        foreach (var node in circuit.Nodes)
        //        {
        //            Assert.True(node.Fingerprint.Length > 0, "A circuit should have a fingerprint.");
        //            Assert.True(node.Ipv4Address.Length > 0 || node.Ipv6Address.Length > 0, "A circuit should have an IPv4 or IPv6 address.");
        //            autoEvent.Set(); // event set
        //        }
        //    }
        //}

        public NSData Cookie => NSData.FromUrl(Configuration.DataDirectory.Append("control_auth_cookie", false));

        public void Exec(Callback callback)
        {
            TORController controller = this.Controller;

            var (success, error) = controller.AuthenticateWithDataAsync(Cookie).Result;
            Assert.True(success);
            Assert.Null(error);

            controller.AddObserverForCircuitEstablished((bool established) =>
            {
                if (!established)
                {
                    return;
                }
                callback();
            });
        }

        public delegate void Callback();

        static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;
    }
}