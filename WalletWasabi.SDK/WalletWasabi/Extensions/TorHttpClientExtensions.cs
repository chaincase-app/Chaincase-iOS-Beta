using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Helpers;
using WalletWasabi.TorSocks5;

public static class TorHttpClientExtensions
{
	/// <remarks>
	/// Throws OperationCancelledException if <paramref name="cancel"/> is set.
	/// </remarks>
	public static async Task<HttpResponseMessage> SendAndRetryAsync(this ITorHttpClient client, HttpMethod method, HttpStatusCode expectedCode, string relativeUri, int retry = 2, HttpContent content = null, CancellationToken cancel = default)
	{
		var requestMessage = new HttpRequestMessage(method, new Uri(client.DestinationUri, relativeUri))
		{
			Content = content
		};
		return await client.SendAndRetryAsync(requestMessage, expectedCode, retry, cancel);
	}

	public static async Task<HttpResponseMessage> SendAndRetryAsync(this ITorHttpClient client, HttpRequestMessage requestMessage, HttpStatusCode expectedCode, int retry = 2, CancellationToken cancel = default)
	{
		HttpResponseMessage response = null;
		while (retry-- > 0)
		{
			response?.Dispose();
			cancel.ThrowIfCancellationRequested();
			response = await client.SendAsync(requestMessage, cancel);
			if (response.StatusCode == expectedCode)
			{
				break;
			}
			try
			{
				if (response.Headers.TryGetValues("X-Hashcash-Challenge", out var challenge))
				{
					requestMessage.Headers.Remove("X-Hashcash");
					requestMessage.Headers.Add("X-Hashcash", HashCashUtils.ComputeFromChallenge(challenge.First()));
				}

				if (!response.Headers.TryGetValues("X-Hashcash-Error", out _))
				{
					await Task.Delay(1000, cancel);
				}
			}
			catch (TaskCanceledException ex)
			{
				throw new OperationCanceledException(ex.Message, ex, cancel);
			}
		}
		return response;
	}
}
