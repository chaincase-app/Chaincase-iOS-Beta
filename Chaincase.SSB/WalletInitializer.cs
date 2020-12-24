using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common;
using Microsoft.Extensions.Hosting;

public class WalletInitializer : IHostedService
{
	private readonly Global _global;

	public WalletInitializer(Global global)
	{
		_global = global;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_ = _global.InitializeNoWalletAsync(cancellationToken);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask; 
	}
}
