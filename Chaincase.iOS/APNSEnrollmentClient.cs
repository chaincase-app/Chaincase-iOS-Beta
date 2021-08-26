using System;
using System.Net;
using System.Net.Http;
using WalletWasabi.Bases;
using WalletWasabi.Backend.Models;
using Chaincase.Common;
using System.Threading.Tasks;
using System.Threading;
using WalletWasabi.Helpers;
using Newtonsoft.Json;
using System.Text;

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
            var request = new DeviceToken
            {
                Token = tokenString,
                Type = isDebug ? TokenType.AppleDebug : TokenType.Apple
            };

            var jsonRequest = JsonConvert.SerializeObject(request, Formatting.None);

            using var response = await TorClient.SendAndRetryAsync(
                HttpMethod.Post,
                HttpStatusCode.OK,
                $"/api/v{ApiVersion}/btc/apntokens",
                content: new StringContent(jsonRequest, Encoding.UTF8, "application/json"),
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
