using System.Threading.Tasks;
using Chaincase.Common;

namespace Chaincase
{
	public class MockTorManager : ITorManager

	{
		public TorState State { get; } = TorState.Connected;

		public void Start(bool ensureRunning, string dataDir)
		{
		}

		public Task StopAsync()
		{
			return Task.CompletedTask;
		}
	}
}
