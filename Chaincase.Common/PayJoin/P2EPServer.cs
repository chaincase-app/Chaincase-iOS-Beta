
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using WalletWasabi.WebClients.PayJoin;
using System.Web;
using Newtonsoft.Json;
using System.Linq;
using NBitcoin;

namespace Chaincase.Common.PayJoin
{
	public class P2EPServer : BackgroundService
	{
		private readonly HttpListener _listener;
		private readonly ITorManager _torManager;
		private readonly P2EPRequestHandler _handler;

		private ChaincaseWalletManager _walletManager { get; }
		private INotificationManager _notificationManager { get; }
		private NBitcoin.Network _network { get; }

		public string ServiceId { get; private set; }
		private readonly int _paymentEndpointPort = 37129;
		public string PaymentEndpoint => $"http://{ServiceId}.onion:{_paymentEndpointPort}";

		public string Password { private get; set; }

		public P2EPServer(ITorManager torManager, P2EPRequestHandler handler)
		{
			_listener = new HttpListener();
			_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
			_listener.Prefixes.Add($"http://*:{_paymentEndpointPort}/");
			_torManager = torManager;
			_handler = handler;
		}

		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			ServiceId = _torManager.CreateHiddenService(); // TODO tor not running must throw
			_listener.Start();
			await base.StartAsync(cancellationToken).ConfigureAwait(false);

			while (!cancellationToken.IsCancellationRequested)
			{
				// ProcessRequest aka P2EPRequestHandler
				var context = await GetHttpContextAsync(cancellationToken).ConfigureAwait(false);
				var request = context.Request;
				var urlParams = context.Request.Url;
				var response = context.Response;
				try
				{
					if (request.HttpMethod != "POST")
					{
						response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
						break;
					}

					using var reader = new StreamReader(request.InputStream);
					string body = await reader.ReadToEndAsync().ConfigureAwait(false);
					var payjoinParams = ParseP2EPQueryString(request.Url.Query);
					// pass the PayJoinClientParameters from the url
					// TODO rather than keep the password in memory...
					//string result = await _handler.HandleAsync(body, cancellationToken, Password).ConfigureAwait(false);
					string result = await _handler.HandleP2EPRequestAsync(originalTx,  clientParams);
					var output = response.OutputStream;
					var buffer = Encoding.UTF8.GetBytes(result);
					await output.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
					await output.FlushAsync(cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception e)
				{
					response.StatusCode = (int)HttpStatusCode.BadRequest;
					response.StatusDescription = e.Message;
				}
				finally
				{
					response.Close();

				}
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			Password = null;
			await base.StopAsync(cancellationToken).ConfigureAwait(false);
			_listener.Stop();
			var serviceId = ServiceId;
			if (!string.IsNullOrWhiteSpace(serviceId))
			{
				_torManager.DestroyHiddenService(serviceId);
				ServiceId = "";
			}
		}

		private async Task<HttpListenerContext> GetHttpContextAsync(CancellationToken cancellationToken)
		{
			var getHttpContextTask = _listener.GetContextAsync();
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

		private static PayjoinClientParameters ParseP2EPQueryString(string queryString)
		{
			var query = HttpUtility.ParseQueryString(queryString);
			string json = JsonConvert.SerializeObject(query.Cast<string>().ToDictionary(k => k, v => query[v]));
			return JsonConvert.DeserializeObject<PayjoinClientParameters>(json);

			//return new PayjoinClientParameters
			//{
			//	MaxAdditionalFeeContribution = query.Get("maxadditionalfeecontribution"),
			//	MinFeeRate = new NBitcoin.FeeRate(Decimal.Parse(query.Get("minfeerate"))),
			//	AdditionalFeeOutputIndex = int.Parse(query.Get("additionalfeeoutputindex")),
			//	DisableOutputSubstitution = bool.Parse(query.Get("additionalfeeoutputindex")),
			//	Version = int.Parse(query.Get("v")
			//}
		}
	}
}