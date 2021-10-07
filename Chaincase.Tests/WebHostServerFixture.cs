// FROM: https://github.com/Revolutionary-Games/ThriveDevCenter/blob/7f1d7dcb2882e7be20a98c22efab30315ae50dea/AutomatedUITests/Fixtures/WebHostServerFixture.cs
// under MIT license: https://github.com/Revolutionary-Games/ThriveDevCenter/blob/7f1d7dcb2882e7be20a98c22efab30315ae50dea/LICENSE
// with additional specific changes for Chaincase

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Chaincase.Tests
{
	/// <summary>
	///   Runs the application in Testing environment for use in unit tests.
	///   From: https://www.meziantou.net/automated-ui-tests-an-asp-net-core-application-with-playwright-and-xunit.htm
	///   with modifications.
	///	  ADDITIONAL MOFIGIED
	/// </summary>
	/// <remarks>
	///   <para>
	///      This currently used a pretty hacked together approach to get this working, see this issue for improving
	///      things: https://github.com/dotnet/aspnetcore/issues/4892
	///   </para>
	/// </remarks>
	public abstract class WebHostServerFixture : IDisposable
	{
		private readonly Lazy<Uri> rootUriInitializer;

		public Uri RootUri => rootUriInitializer.Value;
		public IHost Host { get; set; }


		public WebHostServerFixture()
		{
			rootUriInitializer = new Lazy<Uri>(() => new Uri(StartAndGetRootUri()));
		}

		public virtual void Dispose()
		{
			// Originally StopAsync was called after dispose
			Host?.StopAsync();
			Host?.Dispose();
		}

		protected abstract IHost CreateWebHost();

		private string StartAndGetRootUri()
		{
			// As the port is generated automatically, we can use IServerAddressesFeature to get the actual server URL
			Host = CreateWebHost();
			RunInBackgroundThread(Host.Start);
			return Host.Services.GetRequiredService<IServer>().Features
				.Get<IServerAddressesFeature>()
				.Addresses.Single();
		}

		private static void RunInBackgroundThread(Action action)
		{
			var isDone = new ManualResetEvent(false);

			ExceptionDispatchInfo edi = null;
			new Thread(() =>
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					edi = ExceptionDispatchInfo.Capture(ex);
				}

				isDone.Set();
			}).Start();

			if (!isDone.WaitOne(TimeSpan.FromSeconds(10)))
				throw new TimeoutException("Timed out waiting for: " + action);

			if (edi != null)
				throw edi.SourceException;
		}
	
	}

	public class WebHostServerFixture<TStartup> : WebHostServerFixture
		where TStartup : class
	{
		private readonly Action<IWebHostBuilder> _webhostBuilderConfiguration;

		public WebHostServerFixture(Action<IWebHostBuilder> webhostBuilderConfiguration)
		{
			_webhostBuilderConfiguration = webhostBuilderConfiguration;
		}

		protected override IHost CreateWebHost()
		{
			var solutionFolder = SolutionRootFolderFinder.FindSolutionRootFolder();
			return new HostBuilder()
				.ConfigureHostConfiguration(config =>
				{
					// Make static asset and framework asset serving work
					// For some reason, the test project does not generate a static web asset manifest file, so we will reuse the one built at SSB proj...
					// The caveat is that now we MUST ensure we have built the SSB project before we run tests
					var hax = Directory
						.GetFiles(solutionFolder, "*SSB.StaticWebAssets.xml", SearchOption.AllDirectories).First();


					var inMemoryConfiguration = new Dictionary<string, string>
					{
						{ WebHostDefaults.StaticWebAssetsKey, hax }
					};

					config.AddInMemoryCollection(inMemoryConfiguration);
				})
				.ConfigureWebHost(webHostBuilder =>
				{
					webHostBuilder
						.UseEnvironment("Development")
						// .UseConfiguration(configuration)
						.UseKestrel()
						.UseContentRoot(Path.GetFullPath(Path.Join(solutionFolder, "Chaincase.UI/wwwroot")))
						.UseWebRoot(Path.GetFullPath(Path.Join(solutionFolder, "Chaincase.UI/wwwroot")))
						.UseStaticWebAssets()
						.UseStartup<TStartup>()

						// TODO: the actual server should detect BaseUrl automatically, as it is now hardcoded to be this
						// in appsettings.json. Or alternatively we could "naively" pick an empty port here and pass the url
						// here and also in the configuration above
						// .UseUrls($"http://localhost:5001"))
						.UseUrls("http://127.0.0.1:0");

					_webhostBuilderConfiguration?.Invoke(webHostBuilder);
				}) // :0 allows to choose a port automatically
				.Build();
		}
	}

	public class SolutionRootFolderFinder
	{
		/// <summary>
		///   Extracted method from UseSolutionRelativeContentRoot as we also need solution relative web root
		/// </summary>
		/// <returns></returns>
		public static string FindSolutionRootFolder(string solutionName = "*.sln", string baseDirectory = null)
		{
			if (baseDirectory == null)
				baseDirectory = AppContext.BaseDirectory;

			var directoryInfo = new DirectoryInfo(baseDirectory);
			do
			{
				var solutionPath = Directory.EnumerateFiles(directoryInfo.FullName, solutionName).FirstOrDefault();
				if (solutionPath != null)
				{
					return directoryInfo.FullName;
				}

				directoryInfo = directoryInfo.Parent;
			} while (directoryInfo.Parent != null);

			throw new InvalidOperationException("Solution root could not be located");
		}
	}
}
