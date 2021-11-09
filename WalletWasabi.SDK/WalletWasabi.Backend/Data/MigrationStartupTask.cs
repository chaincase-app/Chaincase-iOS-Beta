using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WalletWasabi.Backend.Polyfills;

namespace WalletWasabi.Backend.Data
{
	public class MigrationStartupTask : IStartupTask
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<MigrationStartupTask> _logger;

		public MigrationStartupTask(IServiceProvider serviceProvider, ILogger<MigrationStartupTask> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		public async Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				// we cannot simply ask for this service to be injected automatically in the ctor as a dependency for
				// initializing IDbContextFactory<WasabiBackendContext> is the Global.Config object, which requires
				// another IStartupTask to run, but RunWithTasksAsync requires that all StartupTask services are loaded
				// at once
				var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<WasabiBackendContext>>();
				_logger.LogInformation($"Migrating database to latest version");
				await using var context = contextFactory.CreateDbContext();
				var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
				_logger.LogInformation(pendingMigrations.Any()
					? $"Running migrations: {string.Join(", ", pendingMigrations)}"
					: $"Database already at latest version");
				await context.Database.MigrateAsync(cancellationToken);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Error on the MigrationStartupTask");
				throw;
			}
		}
	}
}