using System.Threading.Tasks;

namespace Chaincase.Common.Contracts
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

        string CreateHiddenServiceAsync();

        Task DestroyHiddenServiceAsync();
    }
}
