
using Foundation;
using System;
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace TorFramework.Tests
{
	[TestFixture]
	public class TORControllerTests
	{
		private TORConfiguration configuration;
        private TORController controller;

		private void SetUp()
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

        [Test]
        public void TestCookieAutheniticationFailure()
        {

            Assert.True(false);
        }

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
            Assert.True(false);
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
			SetUp();
			Assert.True(false);
		}
	}
}

