using Microsoft.EntityFrameworkCore;

namespace WalletWasabi.Backend.Polyfills
{
	public interface IDbContextFactory<TContext>where TContext : DbContext
	{
		TContext CreateDbContext();
	}
}