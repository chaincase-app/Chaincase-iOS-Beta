using System.Threading.Tasks;
using Chaincase.Common;

namespace Chaincase
{
	public class MockTorManager : ITorManager

	{
		public TorState State { get; } = TorState.Connected;

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
