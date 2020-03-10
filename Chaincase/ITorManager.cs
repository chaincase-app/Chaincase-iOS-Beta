using System;
using System.Net;

namespace Chaincase
{
    enum TorState
    {
        None,
        Started,
        Connected,
        Stopped
    }

    public interface ITorManager
    {
        void Start(bool ensureRunning, string dataDir);

        ITorManager Mock();

        void Stop();
    }
}
