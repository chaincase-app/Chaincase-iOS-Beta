using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WalletWasabi.Backend.Polyfills
{
	public static class Extensions
	{
		public static IServiceCollection AddDbContextFactory<TContext>(
			this IServiceCollection collection,
			Action<DbContextOptionsBuilder, IServiceProvider> optionsAction = null,
			ServiceLifetime contextAndOptionsLifetime = ServiceLifetime.Singleton)
			where TContext : DbContext
		{
			// instantiate with the correctly scoped provider
			collection.Add(new ServiceDescriptor(
				typeof(IDbContextFactory<TContext>),
				sp => new DbContextFactory<TContext>(sp),
				contextAndOptionsLifetime));

			// dynamically run the builder on each request
			collection.Add(new ServiceDescriptor(
				typeof(DbContextOptions<TContext>),
				sp => GetOptions<TContext>(optionsAction, sp),
				contextAndOptionsLifetime));

			return collection;
		}
		/// <summary>
		/// Gets the options for a specific <seealso cref="TContext"/>.
		/// </summary>
		/// <param name="action">Option configuration action.</param>
		/// <param name="sp">The scoped <see cref="IServiceProvider"/>.</param>
		/// <returns>The newly configured <see cref="DbContextOptions{TContext}"/>.</returns>
		private static DbContextOptions<TContext> GetOptions<TContext>(
			Action<DbContextOptionsBuilder, IServiceProvider> action,
			IServiceProvider sp = null) where TContext : DbContext
		{
			var optionsBuilder = new DbContextOptionsBuilder<TContext>();
			if (sp != null)
			{
				optionsBuilder.UseApplicationServiceProvider(sp);
			}
			action?.Invoke(optionsBuilder, sp);

			return optionsBuilder.Options;
		}

	}
}