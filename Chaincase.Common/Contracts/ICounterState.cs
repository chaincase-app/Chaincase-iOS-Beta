using System;

namespace Chaincase.Common.Contracts
{
    public interface ICounterState
    {
        int CurrentCount { get; }
        void IncrementCount();
        event Action StateChanged;
    }
}