using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using WalletWasabi.Services;
using WalletWasabi.Stores;
using WalletWasabi.WebClients.Wasabi;

namespace Chaincase.Common.Services
{
	public class ChaincaseSynchronizer : WasabiSynchronizer
	{
		public ChaincaseSynchronizer(Network network, BitcoinStore bitcoinStore, WasabiClient client)
			: base(network, bitcoinStore, client)
		{
		}

		public ChaincaseSynchronizer(Network network, BitcoinStore bitcoinStore, Func<Uri> baseUriAction, EndPoint torSocks5EndPoint)
			: base(network, bitcoinStore, baseUriAction, torSocks5EndPoint)
		{
		}

		public ChaincaseSynchronizer(Network network, BitcoinStore bitcoinStore, Uri baseUri, EndPoint torSocks5EndPoint)
			: base(network, bitcoinStore, baseUri, torSocks5EndPoint)
		{
		}

		public async Task SleepAsync()
		{
			Interlocked.CompareExchange(ref _running, 2, 1); // If running, make it stopping.
			Cancel?.Cancel();
			while (Interlocked.CompareExchange(ref _running, 3, 0) == 2)
			{
				await Task.Delay(50);
			}
			Cancel?.Dispose();
			Cancel = null;
		}

		public void Resume(TimeSpan requestInterval, TimeSpan feeQueryRequestInterval, int maxFiltersToSyncAtInitialization)
		{
			Interlocked.CompareExchange(ref _running, 3, 1); // if stopped, make it running
			Cancel = new CancellationTokenSource();
			Start(requestInterval, feeQueryRequestInterval, maxFiltersToSyncAtInitialization);
		}
	}
}
