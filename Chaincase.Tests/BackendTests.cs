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

		#endregion
	}
}
