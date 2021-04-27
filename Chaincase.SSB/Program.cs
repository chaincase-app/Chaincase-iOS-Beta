using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services.Mock;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;

namespace Chaincase.SSB
{
	// his class is responsible for launching the UI of the appliication,
	// as well as listening (and optionally responding) to application events
	// from Server Side Blazor
	public class Program
	{
		public static void Main(string[] args)
		{
		var dataDirProvider = new SSBDataDirProvider();
			// analagous to new BlazorApp()
			var hostBuilder = Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.ConfigureAppConfiguration(builder => builder.Add(new JsonConfigurationSource()
					{
						Path = Path.Combine(dataDirProvider.Get(), Config.FILENAME),
						Optional = true
					})).UseStartup<Startup>();
				});

			var host = hostBuilder.Build();

			var dataDir = host.Services.GetRequiredService<IDataDirProvider>().Get();
			Directory.CreateDirectory(dataDir); // I hope this is ok
			var config = host.Services.GetRequiredService<Config>();
			config.LoadOrCreateDefaultFile();
			var uiConfig = host.Services.GetRequiredService<UiConfig>();
			uiConfig.LoadOrCreateDefaultFile();

			var global = host.Services.GetRequiredService<Global>();
			Task.Run(async () => await global.InitializeNoWalletAsync());
			host.Run();
		}
	}
}
