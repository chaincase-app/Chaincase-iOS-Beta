using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using WalletWasabi.Backend.Models;
using WalletWasabi.Bases;
using WalletWasabi.Wallets;

namespace Chaincase.Common.Services
{
	public class MempoolSynchronizer: PeriodicRunner
	{
		private readonly ChaincaseClient _client;
		private readonly ChaincaseBitcoinStore _chaincaseBitcoinStore;
		private KeyValuePair<string, FilterModel>? lastRootFilterKey = null;
		private Dictionary<string, FilterModel>? lastSubFilterKey = null;

		private List<Wallet> _wallets = new List<Wallet>();
		private Dictionary<string, Transaction[]> _buckets;

		protected override async Task ActionAsync(CancellationToken cancel)
		{
			if (_chaincaseBitcoinStore.SmartHeaderChain.HashesLeft > 100)
			{
				return;
			}

			var rootFilterResponse = await _client.GetMempoolRootFilter(cancel);
			if (lastRootFilterKey?.Key == rootFilterResponse.Key)
			{
				return;
			}

			lastRootFilterKey = new KeyValuePair<string, FilterModel>(rootFilterResponse.Key, FilterModel.FromLine(rootFilterResponse.Value));
			lastSubFilterKey = null;
			var rootFilter = FilterModel.FromLine(rootFilterResponse.Value);
			var matchedWallets = new List<Wallet>();
			foreach (var wallet in _wallets)
			{
				if (rootFilter.Filter.MatchAny(wallet.KeyManager.GetPubKeyScriptBytes(), rootFilter.FilterKey))
				{
					matchedWallets.Add(wallet);
				}
			}

			if (matchedWallets.Any())
			{
				return;
			}
			
			var subFilters = await _client.GetMempoolSubFilters(cancel);
			lastSubFilterKey = subFilters.ToDictionary(pair => pair.Key, pair => FilterModel.FromLine(pair.Value));

			var matchedSubFiltersToWallet = new Dictionary<Wallet, List<string>>();
			var matchedSubFilters = new HashSet<string>();
			foreach (var wallet in matchedWallets)
			{
				List<string> matchedFilters = new List<string>();
				foreach (var subFilterKey in lastSubFilterKey)
				{
					if (subFilterKey.Value.Filter.MatchAny(wallet.KeyManager.GetPubKeyScriptBytes(), rootFilter.FilterKey))
					{
						matchedFilters.Add(subFilterKey.Key);
						matchedSubFilters.Add(subFilterKey.Key);
					}
				}

				if (matchedFilters.Any())
				{
					matchedSubFiltersToWallet.Add(wallet, matchedFilters);
				}
			}

			if (matchedSubFilters.Any())
			{
				_buckets = await _client.GetMempoolTransactionBuckets(matchedSubFilters.ToArray(), cancel);
				var txs = _buckets.Values.SelectMany(transactions => transactions);
				foreach (var transaction in txs)
				{
					_chaincaseBitcoinStore.MempoolService.Process(transaction);
				}
			}
		}

		public void Register(Wallet w)
		{
			if (!_wallets.Contains(w))
			{
				_wallets.Add(w);
			}

			if (lastRootFilterKey != null)
			{
				//TODO: Scan all logic
			}
		}
		
		public void UnRegister(Wallet w)
		{
			if (_wallets.Contains(w))
			{
				_wallets.Remove(w);
			}
		}
		
		

		public MempoolSynchronizer(ChaincaseClient client,ChaincaseBitcoinStore chaincaseBitcoinStore) : base(TimeSpan.FromSeconds(30))
		{
			_client = client;
			_chaincaseBitcoinStore = chaincaseBitcoinStore;
		}
	}
}
