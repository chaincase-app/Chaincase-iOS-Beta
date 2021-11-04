using System;
using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WalletWasabi.Backend.Data;
using WalletWasabi.Backend.Models;
using WalletWasabi.Backend.Polyfills;
using WalletWasabi.Helpers;
using WalletWasabi.Tests.XunitConfiguration;
using Xunit;

namespace Chaincase.Tests
{
	public class BackendTests
	{
		#region Chaincase Backend Tests

		[Fact]
		public async Task HashcashTests()
		{
			var challenge = HashCashUtils.GenerateChallenge("chocolate", DateTimeOffset.UtcNow.AddMinutes(5), 20);
			var stamp = HashCashUtils.ComputeFromChallenge(challenge);
			Assert.True(HashCashUtils.Verify(stamp));
		}

		[Fact]
		public async Task NotificationsTests()
		{
			var regtestFixture = new RegTestFixture();
			var jobj = JObject.FromObject(regtestFixture.Global.Config);
			Assert.Equal(0, jobj["HashCashDifficulty"].Value<int>());
			jobj["HashCashDifficulty"] = 10;
			JsonConvert.PopulateObject(jobj.ToString(), regtestFixture.Global.Config);
			Assert.Equal(10, regtestFixture.Global.Config.HashCashDifficulty);
			using var client = new ChaincaseClient(() => new Uri(regtestFixture.BackendEndPoint), null);
			_ = await client.RegisterNotificationTokenAsync(new DeviceToken()
			{
				Status = TokenStatus.New,
				Token = "123456",
				Type = TokenType.AppleDebug
			}, CancellationToken.None);

			var factory = regtestFixture.BackendHost.Services.GetService<IDbContextFactory<WasabiBackendContext>>();
			await using var context = factory.CreateDbContext();
			Assert.NotNull(await context.FindAsync<DeviceToken>("123456"));
			await using var context2 = factory.CreateDbContext();
			_ = await client.RemoveNotificationTokenAsync("123456", CancellationToken.None);
			Assert.Null(await context2.FindAsync<DeviceToken>("123456"));
			_ = await client.RemoveNotificationTokenAsync("123456", CancellationToken.None);
		}

		#endregion
	}
}
