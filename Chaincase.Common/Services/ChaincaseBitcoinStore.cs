using System;
using System.IO;
using System.Threading.Tasks;
using NBitcoin;
using WalletWasabi.Backend.Models;
using WalletWasabi.Blockchain.BlockFilters;
using WalletWasabi.Blockchain.Blocks;
using WalletWasabi.Blockchain.Mempool;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Logging;
using WalletWasabi.Stores;
using WalletWasabi.Wallets;

namespace Chaincase.Common.Services
{
	public class ChaincaseBitcoinStore: BitcoinStore
	{
		private readonly ChaincaseClient _chaincaseClient;

		public ChaincaseBitcoinStore(IndexStore indexStore, AllTransactionStore transactionStore,
			MempoolService mempoolService, ChaincaseClient chaincaseClient) : base(indexStore, transactionStore, mempoolService)
		{
			_chaincaseClient = chaincaseClient;
		}

		public override async Task InitializeAsync()
		{
			using (BenchmarkLogger.Measure())
			{


				async Task<FilterModel> GetHeader()
				{
					try
					{
						var res = await _chaincaseClient.GetLatestMatureHeader();
						return StartingFilters.GetStartingFilter(new SmartHeader(res.MatureHeaderHash, res.MatureHeaderPrevHash, res.MatureHeight, res.MatureHeaderTime));
					}
					catch (Exception e)
					{
						// ignored as this is an optional optimization
					}

					return StartingFilters.GetStartingFilter(Network);
				}
				
				
				


				var initTasks = new[]
				{
					//chaincase: ideally, we fetch the latest mature block filter and pass it here as the starting filter
					IndexStore.InitializeAsync(GetHeader()),
					TransactionStore.InitializeAsync()
				};

				await Task.WhenAll(initTasks).ConfigureAwait(false);

				IsInitialized = true;
			}
		}
	}
}
