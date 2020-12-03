using System;
using Chaincase.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.MobileBlazorBindings;
using Xamarin.Forms;

namespace Chaincase
{
	public class BlazorApp : Application
	{
		private readonly IHost _host;

		public IServiceProvider ServiceProvider => _host.Services;

		public BlazorApp(IFileProvider fileProvider = null, Action<IServiceCollection> configureDI = null)
		{
			var hostBuilder = MobileBlazorBindingsHost.CreateDefaultBuilder()
				.ConfigureServices((hostContext, services) =>
				{
					// Adds web-specific services such as NavigationManager
					services.AddBlazorHybrid();
					services.AddUIServices();
					
					configureDI?.Invoke(services);
				})
				.UseWebRoot("wwwroot");

			if (fileProvider != null)
			{
				hostBuilder.UseStaticFiles(fileProvider);
			}
			else
			{
				hostBuilder.UseStaticFiles();
			}

			_host = hostBuilder.Build();

			MainPage = new ContentPage {Title = "Chaincase"};
			_host.AddComponent<Main>(parent: MainPage);
		}

		protected override void OnStart()
		{
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
		}
	}
}
