using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using WalletWasabi.Backend.Models.Responses;
using WalletWasabi.Blockchain.BlockFilters;
using WalletWasabi.CoinJoin.Client.Clients;
using WalletWasabi.Models;
using WalletWasabi.Tests.XunitConfiguration;
using WalletWasabi.TorSocks5;
using WalletWasabi.WebClients.Wasabi;
using Xunit;

namespace WalletWasabi.Tests.IntegrationTests
{
	[Collection("LiveServerTests collection")]
	public class LiveServerTests
	{
		public LiveServerTests(LiveServerTestsFixture liveServerTestsFixture)
		{
			LiveServerTestsFixture = liveServerTestsFixture;

			var torManager = new TorProcessManager(Global.Instance.TorSocks5Endpoint, Global.Instance.TorLogsFile);
			torManager.Start(ensureRunning: true, dataDir: Path.GetFullPath(AppContext.BaseDirectory));
			Task.Delay(3000).GetAwaiter().GetResult();
		}

		private LiveServerTestsFixture LiveServerTestsFixture { get; }

		#region Blockchain

		[Theory]
		[MemberData(nameof(GetNetworks))]
		public async Task GetFeesAsync(Network network)
		{
			using var client = new WasabiClient(LiveServerTestsFixture.UriMappings[network], Global.Instance.TorSocks5Endpoint);
			var feeEstimationPairs = await client.GetFeesAsync(1000);

			Assert.True(feeEstimationPairs.NotNullAndNotEmpty());
		}

		[Theory]
		[MemberData(nameof(GetNetworks))]
		public async Task GetFiltersAsync(Network network)
		{
			using var client = new WasabiClient(LiveServerTestsFixture.UriMappings[network], Global.Instance.TorSocks5Endpoint);
			var filterModel = StartingFilters.GetStartingFilter(Network.GetNetwork(network.ToString()));

			FiltersResponse filtersResponse = await client.GetFiltersAsync(filterModel.Header.BlockHash, 2);

			Assert.NotNull(filtersResponse);
			Assert.True(filtersResponse.Filters.Count() == 2);
		}

		[Theory]
		[MemberData(nameof(GetNetworks))]
		public async Task GetAllRoundStatesAsync(Network network)
		{
			using var client = new SatoshiClient(LiveServerTestsFixture.UriMappings[network], Global.Instance.TorSocks5Endpoint);
			var states = await client.GetAllRoundStatesAsync();
			Assert.True(states.NotNullAndNotEmpty());
			Assert.True(states.Count() >= 1);
		}

		[Theory]
		[MemberData(nameof(GetNetworks))]
		public async Task GetTransactionsAsync(Network network)
		{
			using var client = new WasabiClient(LiveServerTestsFixture.UriMappings[network], Global.Instance.TorSocks5Endpoint);
			var randomTxIds = Enumerable.Range(0, 20).Select(_ => RandomUtils.GetUInt256());

			var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
				await client.GetTransactionsAsync(network, randomTxIds.Take(4), CancellationToken.None));
			Assert.Equal("Bad Request\nNo such mempool or blockchain transaction. Use gettransaction for wallet transactions.", ex.Message);

			var mempoolTxIds = await client.GetMempoolHashesAsync(CancellationToken.None);
			randomTxIds = Enumerable.Range(0, 5).Select(_ => mempoolTxIds.RandomElement()).Distinct().ToArray();
			var txs = await client.GetTransactionsAsync(network, randomTxIds, CancellationToken.None);
			var returnedTxIds = txs.Select(tx => tx.GetHash());
			Assert.Equal(returnedTxIds.OrderBy(x => x).ToArray(), randomTxIds.OrderBy(x => x).ToArray());
		}

		#endregion Blockchain

		#region Offchain

		[Theory]
		[MemberData(nameof(GetNetworks))]
		public async Task GetExchangeRateAsync(Network network) // xunit wtf: If this function is called GetExchangeRatesAsync, it'll stuck on 1 CPU VMs (Manjuro, Fedora)
		{
			using var client = new WasabiClient(LiveServerTestsFixture.UriMappings[network], Global.Instance.TorSocks5Endpoint);
			var exchangeRates = await client.GetExchangeRatesAsync();

			Assert.True(exchangeRates.NotNullAndNotEmpty());
		}

		#endregion Offchain

		#region Software

		[Theory]
		[MemberData(nameof(GetNetworks))]
		public async Task GetVersionsTestsAsync(Network network)
		{
			using var client = new WasabiClient(LiveServerTestsFixture.UriMappings[network], Global.Instance.TorSocks5Endpoint);

			var versions = await client.GetVersionsAsync(CancellationToken.None);
			Assert.InRange(versions.ClientVersion, new Version(1, 1, 10), new Version(1, 2));
			Assert.InRange(versions.ClientVersion, new Version(1, 1, 10), WalletWasabi.Helpers.Constants.ClientVersion);
			Assert.Equal(3, versions.BackendMajorVersion);
			Assert.Equal(new Version(2, 0), versions.LegalDocumentsVersion);
		}

		[Theory]
		[MemberData(nameof(GetNetworks))]
		public async Task CheckUpdatesTestsAsync(Network network)
		{
			using var client = new WasabiClient(LiveServerTestsFixture.UriMappings[network], Global.Instance.TorSocks5Endpoint);

			var updateStatus = await client.CheckUpdatesAsync(CancellationToken.None);

			var expectedVersion = new Version(2, 0);
			ushort backendVersion = 3;
			Assert.Equal(new UpdateStatus(true, true, expectedVersion, backendVersion), updateStatus);
			Assert.True(updateStatus.BackendCompatible);
			Assert.True(updateStatus.ClientUpToDate);
			Assert.Equal(expectedVersion, updateStatus.LegalDocumentsVersion);
			Assert.Equal(backendVersion, updateStatus.CurrentBackendMajorVersion);

			var versions = await client.GetVersionsAsync(CancellationToken.None);
			Assert.Equal(versions.LegalDocumentsVersion, updateStatus.LegalDocumentsVersion);
		}

		#endregion Software

		public static IEnumerable<object[]> GetNetworks()
		{
			yield return new object[] { Network.Main };
			yield return new object[] { Network.TestNet };
		}
	}
}
