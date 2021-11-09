using Microsoft.EntityFrameworkCore;

namespace WalletWasabi.Backend.Data
{
	public class WasabiDesignTimeDbContextFactory :
		DesignTimeDbContextFactoryBase<WasabiBackendContext>
	{
		protected override WasabiBackendContext CreateNewInstance(DbContextOptions<WasabiBackendContext> options)
		{
			return new WasabiBackendContext(options);
		}
	}
}