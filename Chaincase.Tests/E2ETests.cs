using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Chaincase.SSB;
using Chaincase.UI.ViewModels;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;
using NBitcoin;
using WalletWasabi.Tests.XunitConfiguration;
using Xunit;

namespace Chaincase.Tests
{
	public class E2ETests
	{
		private WebHostServerFixture<Startup> Create(Dictionary<string, string> config,
			Action<IServiceCollection> additionalServiceConfig = null)
		{
			return new WebHostServerFixture<Startup>(builder =>
			{
				builder.ConfigureAppConfiguration((context, configurationBuilder) =>
				{
					configurationBuilder.AddInMemoryCollection(config);
				}).ConfigureServices(
					collection =>
					{
						additionalServiceConfig?.Invoke(collection);

						collection.Replace(ServiceDescriptor.Singleton<IDataDirProvider>(_ =>
							new TestDataDirProvider($"{DateTimeOffset.Now.ToUnixTimeSeconds()}_{Guid.NewGuid()}")));
					});
			});
		}

		[Fact]
		public async Task EnsureAppRuns()
		{
			using var factory = Create(new Dictionary<string, string>());
			using var playwright = await Playwright.CreateAsync();

			var inCi = Environment.GetEnvironmentVariable("IN_CI");

			await using var browser = await playwright.Chromium.LaunchAsync(inCi?.Equals("true") is true
				? new BrowserTypeLaunchOptions()
				: new BrowserTypeLaunchOptions
				{
					Headless = false,
					SlowMo = 100
				});
			var page = await browser.NewPageAsync();
			
			await TestUtils.EventuallyAsync(async () =>
			{
				await page.GotoAsync(factory.RootUri.ToString());
			});
			
			TestUtils.Eventually(() =>
			{
				//ensure that the data dir has been created
				var datadir = factory.Host.Services.GetRequiredService<IDataDirProvider>().Get();
				Assert.True(Directory.Exists(datadir));
			});
			
			//fresh run = landing page default
			Assert.EndsWith("/landing", page.Url);
		}
		
		

		[Fact]
		public async Task SyncWalletFromRecentBlock()
		{
			using var regTestFixture = new RegTestFixture();
			using var factory = Create(new Dictionary<string, string>(), collection =>
			{
				collection.Replace(ServiceDescriptor.Singleton<Config>(provider =>
				{
					var c = new Config(provider.GetService<IDataDirProvider>())
					{
						
						Network = Network.RegTest,
						RegTestBackendUriV3 = regTestFixture.BackendEndPoint
					};
					c.SetP2PEndpoint(regTestFixture.BackendRegTestNode.P2pEndPoint);
					c.ToFile();
					return c;
				}));
			});
			using var playwright = await Playwright.CreateAsync();

			var inCi = Environment.GetEnvironmentVariable("IN_CI");

			await using var browser = await playwright.Chromium.LaunchAsync(inCi?.Equals("true") is true
				? new BrowserTypeLaunchOptions()
				: new BrowserTypeLaunchOptions
				{
					Headless = false,
					SlowMo = 100
				});
			var page = await browser.NewPageAsync();
			
			await TestUtils.EventuallyAsync(async () =>
			{
				await page.GotoAsync(factory.RootUri.ToString());
			});

			var info = await regTestFixture.BackendRegTestNode.RpcClient.GetBlockCountAsync();
			var client = factory.Host.Services.GetRequiredService<ChaincaseClient>();
			var matureHeader = await client.GetLatestMatureHeader();
			Assert.Equal((uint)info, matureHeader.BestHeight);
			Assert.Equal((uint)1, matureHeader.MatureHeight);
			var bStore = factory.Host.Services.GetService<ChaincaseBitcoinStore>();
			
			Assert.Equal(matureHeader.MatureHeight, bStore.IndexStore.StartingHeight);
			Assert.EndsWith("/landing", page.Url);
			await page.RunAndWaitForNavigationAsync(async () =>
				await page.ClickAsync("#btn-create-wallet"));
			var password = Guid.NewGuid().ToString();
			await page.TypeAsync("#txt-password input", password);
			await page.RunAndWaitForNavigationAsync(async () =>
				await page.ClickAsync("#btn-next"));
			await page.TypeAsync("#txt-password input", password);
			await page.RunAndWaitForNavigationAsync(async () =>
				await page.ClickAsync("#btn-load-wallet"));
			Assert.EndsWith("/overview", page.Url);
			while (true)
			{
				Console.WriteLine("filters left:  "  + factory.Host.Services.GetService<StatusViewModel>().FiltersLeft);
				Assert.Equal(0, factory.Host.Services.GetService<StatusViewModel>().FiltersLeft);
			}
			
			
		}
	}
}
