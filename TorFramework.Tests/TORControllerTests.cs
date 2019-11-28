
using Foundation;
using System;
using NUnit.Framework;
using System.Runtime.InteropServices;
using System.Threading;

namespace TorFramework.Tests
{
	[TestFixture]
	public class TORControllerTests
	{
		private TORConfiguration configuration;
        private TORController controller;

        [TestFixtureSetUp]
		protected void SetUp()
		{
			configuration = new TORConfiguration();
			configuration.CookieAuthentication = new Foundation.NSNumber(true); //@YES
			// This goes contrary to the TARGET_IPHONE_SIMULATOR in iCepa's tests.
			configuration.DataDirectory = new NSUrl(System.IO.Path.GetTempPath());
			configuration.ControlSocket = new NSUrl(NSFileManager.DefaultManager.GetCurrentDirectory());
			configuration.Arguments = new string[] { "--ignore-missing-torrc" };

			TORThread thread = new TORThread(configuration);
			thread.Start();
			NSRunLoop.Main.RunUntil(NSDate.FromTimeIntervalSinceNow(0.5f));

            controller = new TORController(configuration.ControlSocket);
        }

        //- (void) testCookieAuthenticationFailure
        //        {
        //            XCTestExpectation *expectation = [self expectationWithDescription:@"authenticate callback"];
        //            [self.controller authenticateWithData:[@"invalid" dataUsingEncoding:NSUTF8StringEncoding]
        //            completion:^(BOOL success, NSError* error) {
        //             XCTAssertFalse(success);
        //             XCTAssertEqualObjects(error.domain, TORControllerErrorDomain);
        //            XCTAssertNotEqual(error.code, 250);
        //            XCTAssertGreaterThan(error.localizedDescription, @"Authentication failed: Wrong length on authentication cookie.");
        //            [expectation fulfill];
        //    }];
    
        //    [self waitForExpectationsWithTimeout:1.0f handler:nil];

        //   }

        //[Test]
        //public void TestCookieAutheniticationFailure()
        //{
        //    //Action<bool, NSError> callback =
        //    var (success, error) = controller.AuthenticateWithDataAsync(new NSString("invalid").Encode(NSStringEncoding.UTF8)).Result;
        //    Assert.False(success);
        //    Assert.True(string.Equals(error.Domain, Constants.TORControllerErrorDomain.ToString()));
        //    Assert.True(error.Code != new nint(250));
        //}

         //- (void) testCookieAuthenticationSuccess
         //   {
         //       XCTestExpectation *expectation = [self expectationWithDescription:@"authenticate callback"];

         //       NSURL *cookieURL = [[[[self class] configuration] dataDirectory] URLByAppendingPathComponent:@"control_auth_cookie"];
         //   NSData* cookie = [NSData dataWithContentsOfURL: cookieURL];
         //   [self.controller authenticateWithData:cookie completion:^(BOOL success, NSError *error) {
         //       XCTAssertTrue(success);
         //       XCTAssertNil(error);
         //       [expectation fulfill];
         //   }];
    
         //   [self waitForExpectationsWithTimeout:1.0f handler:nil];
        //}

        [Test]
        public void TestCookieAuthentiicationSuccess()
        {
            NSUrl cookieUrl = configuration.DataDirectory.Append("control_auth_cookie", false);
            NSData cookie = NSData.FromUrl(cookieUrl);
            var (success, error) = controller.AuthenticateWithDataAsync(cookie).Result;
            Assert.True(success);
            Assert.True(error is null);
            
            //file:///Users/dan/Library/Developer/CoreSimulator/Devices/F56F4323-8547-4BF4-A437-973105124CFF/data/Containers/Data/Application/F9538CF0-5B57-4DCA-8733-5CF7345F6832/tmp.control_auth_cookie/}
            //file:///Users/dan/Library/Developer/CoreSimulator/Devices/F56F4323-8547-4BF4-A437-973105124CFF/data/Containers/Data/Application/A29FD3D0-5104-4F46-BF3B-C7FEE6D5DE1F/tmp.control_auth_cookie/}
        }



        //- (void) testSessionConfiguration
        //        {
        //            XCTestExpectation *expectation = [self expectationWithDescription:@"tor callback"];

            //            TORController *controller = self.controller;

            //    void (^test)(void) = ^{
            //        [controller getSessionConfiguration:^(NSURLSessionConfiguration* configuration) {
            //            NSURLSession* session = [NSURLSession sessionWithConfiguration: configuration];
            //        [[session dataTaskWithURL:[NSURL URLWithString:@"https://facebookcorewwwi.onion/"] completionHandler:^(NSData* data, NSURLResponse* response, NSError* error) {
            //                XCTAssertEqual([(NSHTTPURLResponse *)response statusCode], 200);
            //        XCTAssertNil(error);
            //        [expectation fulfill];
            //            }] resume];
            //        }];
            //    };

            //    NSURL* cookieURL = [[[[self class] configuration] dataDirectory] URLByAppendingPathComponent:@"control_auth_cookie"];
            //    NSData* cookie = [NSData dataWithContentsOfURL: cookieURL];
            //    [controller authenticateWithData:cookie completion:^(BOOL success, NSError *error) {
            //        if (!success)
            //            return;


            //        [controller addObserverForCircuitEstablished:^(BOOL established) {
            //            if (!established)
            //                return;


            //        test();
            //        }];
            //    }];


        [Test]
		public void TestSessionConfiguration()
		{
			Assert.True(false);
		}
	}
}

