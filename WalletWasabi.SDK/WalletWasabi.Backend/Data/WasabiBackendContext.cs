using Microsoft.EntityFrameworkCore;
using WalletWasabi.Backend.Models;

namespace WalletWasabi.Backend.Data
{
	public class WasabiBackendContext : DbContext
	{
		public DbSet<DeviceToken> Tokens { get; set; }

		public WasabiBackendContext(DbContextOptions<WasabiBackendContext> options) : base(options)
		{
		}
	}
}
