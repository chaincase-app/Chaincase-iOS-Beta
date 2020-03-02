using System;
using Xunit;
using TorFramework;
using System.Threading.Tasks;
using ObjCRuntime;
using Foundation;
using System.IO;
using System.Linq;

namespace TorFramework.Tests
{
    public class TORControllerFixture : IDisposable
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
            thread = new TORThread(Configuration);
            thread.Start();

            NSRunLoop.Main.RunUntil(NSDate.FromTimeIntervalSinceNow(0.5f));
        }

        static string NSHomeDirectory() => Directory.GetParent(NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User).First().Path).FullName;

        public void Dispose()
        {
            thread.Cancel();
        }
    }

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
    }
}
