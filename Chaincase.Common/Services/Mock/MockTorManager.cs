using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;

namespace Chaincase.Common.Services.Mock
{
    public class MockTorManager : ITorManager

    {
        public TorState State { get; } = TorState.Connected;
        public Task EnsureRunning()
        {
	        return Task.CompletedTask;
        }

        public Task StartAsync(bool ensureRunning, string dataDir)
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
	        return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
	        return Task.CompletedTask;
        }
    }
}
