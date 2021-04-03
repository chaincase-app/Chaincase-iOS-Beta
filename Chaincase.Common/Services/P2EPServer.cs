using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Chaincase.Common.Contracts;

namespace Chaincase.Common.Services
{
	public class P2EPServer : BackgroundService
	{
		private ITorManager TorManager => Global.TorManager;
		private HttpListener Listener { get; }
		public string ServiceId { get; private set; }
		public Global Global { get; }
		public string PaymentEndpoint => $"http://{ServiceId}.onion:37129";

		public P2EPServer(Global global)
		{
			Global = global;
			Listener = new HttpListener();
			Listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
			Listener.Prefixes.Add($"http://+:37129/");
		}

		public override async Task StartAsync(CancellationToken cancellationToken)
		{
			// Assummming running 
			//while (!Tor)
			//{
			//	await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
			//}
			ServiceId = TorManager.CreateHiddenServiceAsync();

			//await TorControlClient.ConnectAsync().ConfigureAwait(false);
			//await TorControlClient.AuthenticateAsync("MyLittlePonny").ConfigureAwait(false);

			Listener.Start();
			await base.StartAsync(cancellationToken).ConfigureAwait(false);
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			await base.StopAsync(cancellationToken).ConfigureAwait(false);
			Listener.Stop();
			var serviceId = ServiceId;
			if (!string.IsNullOrWhiteSpace(serviceId))
			{
				TorManager.DestroyHiddenServiceAsync(serviceId);
			}
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var handler = new P2EPRequestHandler(Global.Network, Global.WalletManager, Global.Config.PrivacyLevelSome);

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var context = await GetHttpContextAsync(stoppingToken).ConfigureAwait(false);
					var request = context.Request;
					var response = context.Response;

					if (request.HttpMethod == "POST")
					{
						using var reader = new StreamReader(request.InputStream);
						string body = await reader.ReadToEndAsync().ConfigureAwait(false);

						try
						{
							var result = await handler.HandleAsync(body, stoppingToken).ConfigureAwait(false);

							var output = response.OutputStream;
							var buffer = Encoding.UTF8.GetBytes(result);
							await output.WriteAsync(buffer, 0, buffer.Length, stoppingToken).ConfigureAwait(false);
							await output.FlushAsync(stoppingToken).ConfigureAwait(false);
						}
						catch (Exception e)
						{
							response.StatusCode = (int)HttpStatusCode.BadRequest;
							response.StatusDescription = e.Message;
						}
					}
					else
					{
						response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
					}
					response.ContentType = "text/html; charset=UTF-8";
					response.Close();
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
				}
			}
		}

		private async Task<HttpListenerContext> GetHttpContextAsync(CancellationToken cancellationToken)
		{
			var getHttpContextTask = Listener.GetContextAsync();
			var tcs = new TaskCompletionSource<bool>();
			using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state).TrySetResult(true), tcs))
			{
				var firstTaskToComplete = await Task.WhenAny(getHttpContextTask, tcs.Task).ConfigureAwait(false);
				if (getHttpContextTask != firstTaskToComplete)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
			return await getHttpContextTask.ConfigureAwait(false);
		}
	}
}