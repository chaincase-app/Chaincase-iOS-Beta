using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WalletWasabi.Backend.Data
{
	public abstract class DesignTimeDbContextFactoryBase<TContext> :
		IDesignTimeDbContextFactory<TContext> where TContext : DbContext
	{

		public TContext CreateDbContext(string[] args)
		{
			return Create();
		}

		protected abstract TContext CreateNewInstance(
			DbContextOptions<TContext> options);

		public TContext Create()
		{
			return Create(
				"User ID=postgres;Host=127.0.0.1;Port=65466;Database=doesntmatterbecauseitisnotactuallyused;");
		}

		private TContext Create(string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
			{
				throw new ArgumentException(
					$"{nameof(connectionString)} is null or empty.",
					nameof(connectionString));
			}

			var optionsBuilder =
				new DbContextOptionsBuilder<TContext>();

			Console.WriteLine(
				"MyDesignTimeDbContextFactory.Create(string): Connection string: {0}",
				connectionString);

			optionsBuilder.UseNpgsql(connectionString);

			var options = optionsBuilder.Options;

			return CreateNewInstance(options);
		}
	}
}