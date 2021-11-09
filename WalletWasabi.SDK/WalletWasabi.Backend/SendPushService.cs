using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WalletWasabi.Backend.Data;
using WalletWasabi.Backend.Models;
using WalletWasabi.Backend.Polyfills;
using WalletWasabi.Logging;

namespace WalletWasabi.Backend
{
	public class SendPushService
	{
		private readonly IDbContextFactory<WasabiBackendContext> ContextFactory;

		private string _keyPath = "/home/staging/AuthKey_4L3728R8LJ.p8";
		private string _auth_key_id = "4L3728R8LJ";
		private string _teamId = "9Z72DXKVXK"; // Chaincase LLC
		private string _bundleId = "cash.chaincase.testnet"; // APNs Development iOS
		private string _payload = @"{
				""aps"": {
					""content-available"": 1
				},
				""cj"": 1
			}";

		public SendPushService(IDbContextFactory<WasabiBackendContext> contextFactory)
		{
			ContextFactory = contextFactory;
		}

		private string GenerateAuthenticationHeader()
		{
			var headerBytes = JsonSerializer.SerializeToUtf8Bytes(new {
				alg = "ES256",
				kid = _auth_key_id
			});
			var header = Convert.ToBase64String(headerBytes);

			var claimsBytes = JsonSerializer.SerializeToUtf8Bytes(new
			{
				iss = _teamId,
				iat = DateTimeOffset.Now.ToUnixTimeSeconds()
			});
			var claims = Convert.ToBase64String(claimsBytes);

			var p8KeySpan = GetBytesFromPem(_keyPath);
			var signer = ECDsa.Create();
			signer.ImportPkcs8PrivateKey(p8KeySpan, out _);
			var dataToSign = Encoding.UTF8.GetBytes($"{header}.{claims}");
			var signatureBytes = signer.SignData(dataToSign, HashAlgorithmName.SHA256);

			var signature = Convert.ToBase64String(signatureBytes);

			return $"{header}.{claims}.{signature}";
		}

		/// <summary>
		/// Apple gives us a APNs Auth Key to sign jwts with in a p8 pem file.
		/// This method reads that
		/// </summary>
		public static byte[] GetBytesFromPem(string pemFile)
		{
			var p8File = File.ReadAllLines(pemFile);
			var p8Key = p8File.Skip(1).SkipLast(1); // Remove PEM bookends
			var base64Key = string.Join("", p8Key);
			return Convert.FromBase64String(base64Key);
		}

		// https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/sending_notification_requests_to_apns/
		public async Task SendNotificationsAsync(bool isDebug)
		{
			await using var context = ContextFactory.CreateDbContext();
			var client = new HttpClient();
			client.DefaultRequestVersion = HttpVersion.Version20;
			var content = new StringContent(_payload, Encoding.UTF8, "application/json");
			client.DefaultRequestHeaders.Add("apns-topic", _bundleId);
			client.DefaultRequestHeaders.Add("apns-push-type", "background");
			client.DefaultRequestHeaders.Add("apns-priority", "5"); // background push MUST be 5
			client.DefaultRequestHeaders.Add("apns-expiration", "0"); // attempt delivery only once

			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", GenerateAuthenticationHeader());

			var server = isDebug ? "api.sandbox" : "api";
			var tokenType = isDebug ? TokenType.AppleDebug : TokenType.Apple;
			Logger.LogInfo($"SendNotificationsAsync isDebug: {isDebug}");
			var tokens = await context.Tokens
				.Where(t => t.Status != TokenStatus.Invalid && t.Type == tokenType)
				.ToListAsync();

			foreach (var token in tokens)
			{
				await SendNotificationAsync(token, server, context, content, client);
			}
			await context.SaveChangesAsync();
		}

		public async Task SendNotificationAsync(DeviceToken token, string server, WasabiBackendContext context, StringContent content, HttpClient client)
		{
			var url = $"https://{server}.push.apple.com/3/device/{token.Token}";
			var res = await client.PostAsync(url, content);

			if (!res.IsSuccessStatusCode)
			{
				Logger.LogError($"HttpPost to APNs failed: {await res.Content.ReadAsStringAsync()} {token.Token}");
			}
			else
			{
				token.Status = TokenStatus.Valid;
			}

			if (res.StatusCode is HttpStatusCode.BadRequest || res.StatusCode is HttpStatusCode.Gone)
			{
				if (res.ReasonPhrase == "BadDeviceToken")
				{
					token.Status = TokenStatus.Invalid;
				}
			}
		}
	}
}

