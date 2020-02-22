
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

namespace TorFramework.Tests
{

    interface ITORControllerTests
    {
        TORController controller { get;  }// @property (nonatomic, strong) TORController *controller;
        NSData cookie { get; } // @property (readonly) NSData *cookie;
    }

	[TestFixture]
	public class TORControllerTests
	{

        static TORConfiguration configuration
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
                                                                         // This goes contrary to the TARGET_IPHONE_SIMULATOR in iCepa's tests.
                configuration.DataDirectory = new NSUrl(Path.GetTempPath());
                configuration.ControlSocket = NSUrl.CreateFileUrl(new string[] { homeDirectory, ".tor/control_port" });
                configuration.Arguments = new string[] { "--ignore-missing-torrc" };
                return configuration;
            }
        }

        [TestFixtureSetUp]
		protected static void SetUpOnce()
		{

			TORThread thread = new TORThread(configuration);
			thread.Start();

			NSRunLoop.Main.RunUntil(NSDate.FromTimeIntervalSinceNow(0.5f));
        }

        public TORController controller;

        [SetUp]
        public void SetUp()
        {
            controller = new TORController(configuration.ControlSocket);
        }

        [Test]
        public async void TestCookieAutheniticationFailure()
        {
            //Action<bool, NSError> callback =
            var (success, error) = await controller.AuthenticateWithDataAsync(new NSString("invalid").Encode(NSStringEncoding.UTF8));
            Assert.False(success);
            Assert.True(string.Equals(error.Domain, Constants.TORControllerErrorDomain.ToString()));
            Assert.True(error.Code != new nint(250));
            //Assert.(error.LocalizedDescription.Equals("Authentication Failed: Wrong length of authentication cookie."));
        }

        [Test]
        public async void TestCookieAuthenticationSuccess()
        {
            NSUrl cookieUrl = configuration.DataDirectory.Append("control_auth_cookie", false);
            NSData cookie = NSData.FromUrl(cookieUrl);
            var (success, error) = await controller.AuthenticateWithDataAsync(cookie);
            Assert.True(success);
            Assert.True(error is null);
        }



        //- (void) testSessionConfiguration
        //{
        //    XCTestExpectation *expectation = [self expectationWithDescription:@"tor callback"];

        //        TORController *controller = self.controller;

        //void (^test)(void) = ^{
        //    [controller getSessionConfiguration:^(NSURLSessionConfiguration* configuration) {
        //        NSURLSession* session = [NSURLSession sessionWithConfiguration: configuration];
        //    [[session dataTaskWithURL:[NSURL URLWithString:@"https://facebookcorewwwi.onion/"] completionHandler:^(NSData* data, NSURLResponse* response, NSError* error) {
        //            XCTAssertEqual([(NSHTTPURLResponse *)response statusCode], 200);
        //    XCTAssertNil(error);
        //    [expectation fulfill];
        //        }] resume];
        //    }];
        //};

        //NSURL* cookieURL = [[[[self class] configuration] dataDirectory] URLByAppendingPathComponent:@"control_auth_cookie"];
        //NSData* cookie = [NSData dataWithContentsOfURL: cookieURL];
        //[controller authenticateWithData:cookie completion:^(BOOL success, NSError *error) {
        //    if (!success)
        //        return;


        //    [controller addObserverForCircuitEstablished:^(BOOL established) {
        //        if (!established)
        //            return;


        //    test();
        //    }];
        //}];


        //      [Test]
        //public void TestSessionConfiguration()
        //{
        //	Assert.True(false);
        //}

        public NSData cookie => NSData.FromUrl(NSUrl.CreateFileUrl(new string[] { configuration.DataDirectory.ToString(), "control_auth_cookie" }));

        static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;
    }
}

