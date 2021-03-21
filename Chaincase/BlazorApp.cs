using System;
using System.IO;
using System.Threading.Tasks;
using Chaincase.Background;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Services;
using Chaincase.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.MobileBlazorBindings;

using Splat;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Logging;
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

					services.AddScoped<IClipboard, XamarinClipboard>();
					services.AddSingleton<IDataDirProvider, XamarinDataDirProvider>();
					services.AddSingleton<IMainThreadInvoker, XamarinMainThreadInvoker>();
					services.AddSingleton<IShare, XamarinShare>();
					services.AddSingleton<IThemeManager, XamarinThemeManager>();


					configureDI?.Invoke(services);

					services.AddUIServices();
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


			MainPage = new ContentPage { Title = "Chaincase" };

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			_host.AddComponent<Main>(parent: MainPage);
		}

		public void InitializeNoWallet()
		{
			// This relies on Global registered
			var message = new InitializeNoWalletTaskMessage();
			MessagingCenter.Send(message, "InitializeNoWalletTaskMessage");
		}

		protected override void OnSleep()
		{
			// Execute Sleeping Background Task
			var message = new OnSleepingTaskMessage();
			MessagingCenter.Send(message, "OnSleepingTaskMessage");
		}

		protected override void OnResume()
		{
			Resuming += OnResuming;
			// Execute Async code
			Resuming(this, EventArgs.Empty);
		}

		private event EventHandler Resuming = delegate { };

		private async void OnResuming(object sender, EventArgs args)
		{
			//unsubscribe from event
			Resuming -= OnResuming;

			//perform non-blocking actions
			await _host.Services.GetRequiredService<Global>().OnResuming();
		}

		private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			Logger.LogWarning(e?.Exception, "UnobservedTaskException");
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Logger.LogWarning(e?.ExceptionObject as Exception, "UnhandledException");
		}

		public static async Task LoadWalletAsync()
		{
			var global = Locator.Current.GetService<Global>();
			string walletName = global.Network.ToString();
			KeyManager keyManager = global.WalletManager.GetWalletByName(walletName).KeyManager;
			if (keyManager is null)
			{
				return;
			}

			try
			{
				global.Wallet = await global.WalletManager.StartWalletAsync(keyManager);
				// Successfully initialized.
			}
			catch (OperationCanceledException ex)
			{
				Logger.LogTrace(ex);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
			}
		}
	}
}
