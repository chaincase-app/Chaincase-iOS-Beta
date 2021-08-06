using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Chaincase.Common.Contracts
{
    public enum TorState
    {
        None,
        Started,
        Connected,
        Stopped
    }

    public abstract class BaseTorManager : ITorManager
    {
	    private readonly Config _config;
	    private Task StartTask;
	    public BaseTorManager(Config config)
	    {
		    _config = config;
	    }
	    public async Task StartAsync(CancellationToken cancellationToken)
	    {
		    if (StartTask != null)
		    {
			    await StartTask;
		    }
		    if (_config.UseTor && State != TorState.Started && State != TorState.Connected)
		    {
			    StartTask = StartAsyncCore(cancellationToken);
		    }
	    }

	    public abstract Task StartAsyncCore(CancellationToken cancellationToken);
	    public abstract Task StopAsyncCore(CancellationToken cancellationToken);

	    public async Task StopAsync(CancellationToken cancellationToken)
	    {
		    StartTask = null;
		    if (State != TorState.None && State != TorState.Stopped) // OnionBrowser && Dispose@Global
		    {
			    await StopAsyncCore(cancellationToken);
		    }
	    }

	    public abstract TorState State { get; set; }
	    public abstract Task EnsureRunning();
    }

    public interface ITorManager: IHostedService
    {
        TorState State { get; }

        Task EnsureRunning();
    }
}
