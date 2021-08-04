using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common.Models;
using WalletWasabi.Bases;
using WalletWasabi.Helpers;

namespace Chaincase.Common.Services
{
	public class ChaincaseClient : TorDisposableBase
	{
		/// <inheritdoc/>
		public ChaincaseClient(Func<Uri> baseUriAction, EndPoint torSocks5EndPoint) : base(baseUriAction, torSocks5EndPoint)
		{
		}

		public static ushort ApiVersion { get; private set; } = ushort.Parse(Constants.BackendMajorVersion);

		// <remarks>
		/// Throws OperationCancelledException if <paramref name="cancel"/> is set.
		/// </remarks>
		public async Task<LatestMatureHeaderResponse> GetLatestMatureHeader(CancellationToken cancel = default)
		{
			using var response = await TorClient.SendAndRetryAsync(
				HttpMethod.Get,
				HttpStatusCode.OK,
				$"/api/v{ApiVersion}/btc/blockchain/latest-mature-header",
				cancel: cancel);
			if (response.StatusCode == HttpStatusCode.NoContent)
			{
				return null;
			}
			if (response.StatusCode != HttpStatusCode.OK)
			{
				await response.ThrowRequestExceptionFromContentAsync();
			}

			using HttpContent content = response.Content;
			var ret = await content.ReadAsJsonAsync<LatestMatureHeaderResponse>();
			return ret;
		}
	}
}
