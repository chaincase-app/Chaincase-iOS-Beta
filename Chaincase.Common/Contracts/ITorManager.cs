using System.Threading.Tasks;

namespace Chaincase.Common
{
    public enum TorState
    {
        None,
        Started,
        Connected,
        Stopped
    }

    public interface ITorManager
    {
        TorState State { get; }
    
        void Start(bool ensureRunning, string dataDir);

        ITorManager Mock();

        Task StopAsync();
    }
}
