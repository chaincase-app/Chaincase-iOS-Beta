using System;
using Hara.Abstractions.Contracts;

namespace Hara.Abstractions.Services
{
    public class CounterState : ICounterState
    {
        public int CurrentCount { get; private set; }

        public void IncrementCount()
        {
            CurrentCount++;
            StateChanged?.Invoke();
        }

        public event Action StateChanged;
    }
}
