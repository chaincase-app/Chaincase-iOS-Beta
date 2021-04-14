using System;
using NBitcoin.Protocol;
using WalletWasabi.Blockchain.TransactionBroadcasting;

namespace Chaincase.Common.Contracts
{
	public class AppInitializedEventArgs : EventArgs
    {
        public NodesGroup Nodes { get; set; }
        public TransactionBroadcaster TransactionBroadcaster { get; set; }

        public AppInitializedEventArgs(Global global)
        {
            Nodes = global.Nodes;
            TransactionBroadcaster = global.TransactionBroadcaster;
        }
    }
}
