using System.Threading.Tasks;
using Chaincase.Common.Contracts;

namespace Chaincase.Common.Services.Mock
{
    public class MockTorManager : ITorManager

    {
        public TorState State { get; } = TorState.Connected;

		public string CreateHiddenService()
		{
            return "mock.onion";
        }

		public void DestroyHiddenService(string serviceId)
		{
            return;
        }

		public Task StartAsync(bool ensureRunning, string dataDir)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }
}
