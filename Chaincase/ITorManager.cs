using System;
using System.Net;
using System.Threading.Tasks;

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

        public void StartMonitor(TimeSpan torMisbehaviorCheckPeriod, TimeSpan checkIfRunningAfterTorMisbehavedFor, string dataDirToStartWith, Uri fallBackTestRequestUri);

        Task StopAsync();
    }
}
