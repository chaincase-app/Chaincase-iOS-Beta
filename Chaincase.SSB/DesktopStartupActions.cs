using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chaincase.SSB
{
	public class DesktopStartupActions : IHostedService
	{
		private readonly Global _global;
		private readonly ILogger<DesktopStartupActions> _logger;

		public DesktopStartupActions(Global global, ILogger<DesktopStartupActions> logger)
		{
			_global = global;
			_logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Initializing startup logic");

			_ = _global.InitializeNoWalletAsync(cancellationToken);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
