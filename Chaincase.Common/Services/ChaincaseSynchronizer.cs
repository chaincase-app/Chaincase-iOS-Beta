using System;
using System.Collections.Generic;
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
		public int GetMaxFilterFetch()
		{
			switch (Network.ChainName)
			{
				case { } chainName when chainName == ChainName.Mainnet:
					return 1000;

				case { } chainName when chainName == ChainName.Regtest:
					return 10;

				case { } chainName when chainName == ChainName.Testnet:
				default:
					return 10000;
			}
		}

		private TimeSpan GetRequestInterval()
		{
			return TimeSpan.FromSeconds(Network == Network.RegTest ? 5 : 30);
		}

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
				await Task.Delay(50); // wait for Start() loop to Cancel
			}
			Cancel?.Dispose();
			Cancel = null;
		}

		public void Resume( )
		{
			Interlocked.CompareExchange(ref _running, 0, 3); // If stopped, make it not started
			Cancel = new CancellationTokenSource();
			Start();
		}

		public void Start()
		{
			base.Start(GetRequestInterval(), TimeSpan.FromMinutes(5),GetMaxFilterFetch());
		}
		
		public void Restart()
		{
			CreateNew(Network, BitcoinStore, WasabiClient);
			Start();
		}
	}
}
