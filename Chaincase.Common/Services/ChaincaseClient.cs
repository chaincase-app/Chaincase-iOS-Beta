using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common.Models;
using Newtonsoft.Json.Linq;
using WalletWasabi.Backend.Models;
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
		
		public async Task<string> RegisterNotificationTokenAsync(DeviceToken deviceToken, CancellationToken cancel)
		{
			using var response = await TorClient.SendAndRetryAsync(
				HttpMethod.Put,
				HttpStatusCode.OK,
				$"/api/v{ApiVersion}/notificationTokens",
				2,
				new StringContent(JObject.FromObject(deviceToken).ToString(), Encoding.UTF8,
					"application/json")
				, cancel).ConfigureAwait(false);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				await response.ThrowRequestExceptionFromContentAsync();
			}

			using HttpContent content = response.Content;
			var ret = await content.ReadAsStringAsync().ConfigureAwait(false);
			return ret;
		}

		public async Task<string> RemoveNotificationTokenAsync(string deviceToken, CancellationToken cancel)
		{
			using var response = await TorClient.SendAndRetryAsync(
				HttpMethod.Delete,
				HttpStatusCode.OK,
				$"/api/v{ApiVersion}/notificationTokens/{deviceToken}",
				2,null, cancel).ConfigureAwait(false);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				await response.ThrowRequestExceptionFromContentAsync();
			}
			using HttpContent content = response.Content;
			var ret = await content.ReadAsStringAsync().ConfigureAwait(false);
			return ret;
		}
	}
}
