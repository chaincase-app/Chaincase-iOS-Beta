using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using WalletWasabi.TorSocks5;

namespace Chaincase.SSB
{
	public class DesktopTorManager : BaseTorManager
	{
		private readonly IDataDirProvider _dataDirProvider;
		private TorProcessManager _torProcessManager;

		public DesktopTorManager(Global global, Config config, IDataDirProvider dataDirProvider) : base(global, config)
		{
			_dataDirProvider = dataDirProvider;
		}

		public override Task StartAsyncCore(CancellationToken cancellationToken)
		{
			_torProcessManager ??= _config.UseTor
				? new TorProcessManager(_config.TorSocks5EndPoint, null)
				: TorProcessManager.Mock();
			_torProcessManager.Start(false, _dataDirProvider.Get());
			return Task.CompletedTask;
		}

		public override Task StopAsyncCore(CancellationToken cancellationToken)
		{
			return _torProcessManager.StopAsync();
		}

		public override TorState State => _torProcessManager?.IsRunning is true ? TorState.Connected : TorState.None;

		public override Task EnsureRunning()
		{
			_torProcessManager.Start(true, _dataDirProvider.Get());
			return Task.CompletedTask;
		}
	}
}
