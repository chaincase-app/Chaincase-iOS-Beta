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
    
        Task StopAsync();

        Task StartAsync(bool ensureRunning, string dataDir);
    }
}
