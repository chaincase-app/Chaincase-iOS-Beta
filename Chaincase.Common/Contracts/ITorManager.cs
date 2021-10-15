using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NBitcoin;
using WalletWasabi.Logging;

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
	    private readonly Global _global;
	    protected readonly Config _config;
	    private Task StartTask;
	    public BaseTorManager(Global global, Config config)
	    {
		    _global = global;
		    _config = config;

		    _global.Resumed += (sender, args) => StartAsync(_global.ResumeCts.Token);
		    _global.Slept += (sender, args) => StopAsync(_global.SleepCts.Token);
	    }
	    public async Task StartAsync(CancellationToken cancellationToken)
	    {
		    if (StartTask != null)
		    {
			    await StartTask.WithCancellation(cancellationToken);
		    }
		    if (_config.UseTor && State != TorState.Started && State != TorState.Connected)
		    {
			    StartTask = StartAsyncCore(cancellationToken).ContinueWith(task =>
			    {
				    Logger.LogInfo($"{nameof(ITorManager)} is initialized.");
			    }, cancellationToken);
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

	    public abstract TorState State { get;}
	    public abstract Task EnsureRunning();
    }

    public interface ITorManager: IHostedService
    {
        TorState State { get; }

        Task EnsureRunning();
    }
}
