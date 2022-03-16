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

        // <summary>
        // Starts your v3 Tor Hidden Service
        // </summary>
        string CreateHiddenService();

        void DestroyHiddenService(string serviceId);
    }
}
