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

namespace Chaincase.Common.Services
{
	public class ChaincaseBitcoinStore: BitcoinStore
	{
		private readonly string _workFolderPath;
		private readonly ChaincaseClient _chaincaseClient;

		public ChaincaseBitcoinStore(string workFolderPath, Network network, IndexStore indexStore, AllTransactionStore transactionStore, MempoolService mempoolService, ChaincaseClient chaincaseClient) : base(workFolderPath, network, indexStore, transactionStore, mempoolService)
		{
			_workFolderPath = workFolderPath;
			_chaincaseClient = chaincaseClient;
		}

		public override async Task InitializeAsync()
		{
			using (BenchmarkLogger.Measure())
			{
				var networkWorkFolderPath = Path.Combine(_workFolderPath, Network.ToString());
				var indexStoreFolderPath = Path.Combine(networkWorkFolderPath, "IndexStore");


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
					IndexStore.InitializeAsync(indexStoreFolderPath, GetHeader()),
					TransactionStore.InitializeAsync(networkWorkFolderPath, Network)
				};

				await Task.WhenAll(initTasks).ConfigureAwait(false);

				IsInitialized = true;
			}
		}
	}
}
