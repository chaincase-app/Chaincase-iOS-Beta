using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
namespace Chaincase
{
	public class BlockCypherClient : IDisposable
	{
		public Network Network { get; }
		private HttpClient HttpClient { get; }
		public Uri BaseAddress => HttpClient.BaseAddress;

		public BlockCypherClient(Network network, HttpMessageHandler handler = null, bool disposeHandler = false)
		{
			Network = network ?? throw new ArgumentNullException(nameof(network));
			if (handler != null)
			{
				HttpClient = new HttpClient(handler, disposeHandler);
			}
			else
			{
				HttpClient = new HttpClient();
			}
			if (network == Network.Main)
			{
				HttpClient.BaseAddress = new Uri("http://api.blockcypher.com/v1/btc/main");
			}
			else if (network == Network.TestNet)
			{
				HttpClient.BaseAddress = new Uri("http://api.blockcypher.com/v1/btc/test3");
			}
			else
			{
				throw new NotSupportedException($"{network} is not supported");
			}
		}

		public async Task<String> GetGeneralInformationAsync(CancellationToken cancel)
		{
			using (HttpResponseMessage response =
					 await HttpClient.GetAsync("", HttpCompletionOption.ResponseContentRead, cancel))
			{
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new HttpRequestException(response.StatusCode.ToString());
				}

				string jsonString = await response.Content.ReadAsStringAsync();
				return jsonString;
			}
		}

		#region IDisposable Support

		private bool _disposedValue = false; // To detect redundant calls

		public virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects).
					HttpClient?.Dispose();
				}

				// free unmanaged resources (unmanaged objects) and override a finalizer below.
				// set large fields to null.

				_disposedValue = true;
			}
		}

		// override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~BlockCypherClient() {
		// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion IDisposable Support
	}
}
