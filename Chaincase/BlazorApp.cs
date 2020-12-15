using System;
using System.IO;
using System.Threading.Tasks;
using Chaincase.Background;
using Chaincase.Common;
using Chaincase.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.MobileBlazorBindings;
using ReactiveUI;

using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
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
					services.AddUIServices();
					services.UseMicrosoftDependencyResolver();

					configureDI?.Invoke(services);

					services.AddSingleton<Global, Global>();
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

			// This relies on Global registered
			var message = new InitializeNoWalletTaskMessage();
			MessagingCenter.Send(message, "InitializeNoWalletTaskMessage");

			MainPage = new ContentPage {Title = "Chaincase"};

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			_host.AddComponent<Main>(parent: MainPage);
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
			await Locator.Current.GetService<Global>().OnResuming();
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

		private bool WalletExists()
		{
			var global = Locator.Current.GetService<Global>();
			var walletName = global.Network.ToString();
			(string walletFullPath, _) = global.WalletManager.WalletDirectories.GetWalletFilePaths(walletName);
			return File.Exists(walletFullPath);
		}
	}
}
