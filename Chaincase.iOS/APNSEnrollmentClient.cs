using System;
using System.Net;
using System.Net.Http;
using WalletWasabi.Bases;
using WalletWasabi.Backend.Models;
using Chaincase.Common;
using System.Threading.Tasks;
using System.Threading;
using WalletWasabi.Helpers;

namespace Chaincase.iOS
{
    public class APNSEnrollmentClient : TorDisposableBase
    {
        public APNSEnrollmentClient(Config config)
            : base(config.GetCurrentBackendUri(), config.TorSocks5EndPoint)
        {
        }

        public static ushort ApiVersion { get; private set; } = ushort.Parse(Constants.BackendMajorVersion);

        public async Task StoreTokenAsync(string tokenString, bool isDebug = false, CancellationToken cancel = default)
        {
            var request = new AppleDeviceToken
            {
                Token = tokenString,
                IsDebug = isDebug
            };
            using var response = await TorClient.SendAndRetryAsync(
                HttpMethod.Post,
                HttpStatusCode.OK,
                $"/api/v{ApiVersion}/btc/apntokens",
                content: request.ToHttpStringContent(),
                cancel: cancel);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return;
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                await response.ThrowRequestExceptionFromContentAsync();
            }
        }
    }
}
