using System.Threading.Tasks;
using Chaincase.Common.Contracts;

namespace Chaincase.SSB
{
	public class MockTorManager : ITorManager

	{
		public TorState State { get; } = TorState.Connected;

		public string CreateHiddenServiceAsync()
		{
			throw new System.NotImplementedException();
		}

		public void DestroyHiddenServiceAsync(string serviceId)
		{
			throw new System.NotImplementedException();
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
