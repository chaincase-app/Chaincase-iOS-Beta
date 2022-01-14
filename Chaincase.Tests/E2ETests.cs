using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
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
using Moq;
using NBitcoin;
using NBitcoin.RPC;
using WalletWasabi.BitcoinCore;
using WalletWasabi.Blockchain.Mempool;
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


			await using var browser = await playwright.Chromium.LaunchAsync(TestUtils.InCi
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
	}
}
