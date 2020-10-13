using System;
using WalletWasabi.CoinJoin.Common.Models;

namespace Chaincase.Models
{
    public struct RoundPhaseState
    {
        public RoundPhaseState(RoundPhase phase, bool error)
        {
            Phase = phase;
            Error = error;
        }

        public RoundPhase Phase { get; }

        public bool Error { get; }
    }
}
