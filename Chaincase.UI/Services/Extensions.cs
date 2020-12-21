using Chaincase.Common;
using Chaincase.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;

namespace Chaincase.UI.Services
{
	public static class Extensions
	{
		public static IServiceCollection AddUIServices(this IServiceCollection collection)
		{
			collection.AddSingleton<UIStateService>();


			// set Locator to reference this container, too
			var resolver = Locator.CurrentMutable;
			resolver.InitializeSplat();
			resolver.InitializeReactiveUI();

			collection.AddSingleton<IndexViewModel>();
			collection.AddSingleton<SendViewModel>();
			collection.AddTransient<LoadWalletViewModel>();
			collection.AddTransient<WalletInfoViewModel>();
			collection.AddTransient<NewPasswordViewModel>();

			collection.AddSingleton<Global, Global>();
			return collection;
		}
	}
}
