using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Chaincase.SSB
{
	// his class is responsible for launching the UI of the appliication,
	// as well as listening (and optionally responding) to application events
	// from Server Side Blazor
	public class Program
	{
		public static async Task Main(string[] args)
		{
			// analagous to new BlazorApp()
			await CreateHostBuilder(args).RunConsoleAsync();
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
		}
	}
}
