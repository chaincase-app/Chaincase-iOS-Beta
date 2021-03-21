using BlazorDownloadFile;
using Chaincase.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Chaincase.UI.Services;
using Splat.Microsoft.Extensions.DependencyInjection;


namespace Chaincase.SSB
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddRazorPages();
			services.AddDataProtection();
			services.AddServerSideBlazor();
			services.UseMicrosoftDependencyResolver();
			services.AddUIServices();
			services.AddScoped<IClipboard, JSClipboard>();
			services.AddTransient<IThemeManager, MockThemeManager>();
			services.AddScoped<IHsmStorage, JsInteropSecureConfigProvider>();
			services.AddScoped<IShare, SSBShare>();
			services.AddSingleton<IDataDirProvider, SSBDataDirProvider>();
			services.AddSingleton<IMainThreadInvoker, SSBMainThreadInvoker>();
			services.AddSingleton<ITorManager, MockTorManager>();
			services.AddSingleton<INotificationManager, MockNotificationManager>();
			services.AddHostedService<WalletInitializer>();
			services.AddBlazorDownloadFile();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapBlazorHub();
				endpoints.MapFallbackToPage("/_Host");
			});
		}
	}
}
