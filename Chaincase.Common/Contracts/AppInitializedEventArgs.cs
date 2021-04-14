using System;
using NBitcoin.Protocol;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using WalletWasabi.Blockchain.TransactionBroadcasting;
using WalletWasabi.Services;

namespace Chaincase.Common.Contracts
{
    public class AppInitializedEventArgs : EventArgs
    {
        public NodesGroup Nodes { get; set; }
        public WasabiSynchronizer Synchronizer { get; set; }
        public FeeProviders FeeProviders { get; set; }
        public TransactionBroadcaster TransactionBroadcaster { get; set; }

        public AppInitializedEventArgs(Global global)
		{
            Nodes = global.Nodes;
            Synchronizer = global.Synchronizer;
            FeeProviders = global.FeeProviders;
            TransactionBroadcaster = global.TransactionBroadcaster;
		}
    }
}
