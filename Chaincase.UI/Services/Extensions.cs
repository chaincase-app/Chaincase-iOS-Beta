using Chaincase.Common;
using Chaincase.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using QRCoder;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace Chaincase.UI.Services
{
	public static class Extensions
	{
		public static IServiceCollection AddUIServices(this IServiceCollection services)
		{
			services.AddSingleton<UIStateService>();

			// set Locator to reference this container, too
			services.UseMicrosoftDependencyResolver();
			var resolver = Locator.CurrentMutable;
			resolver.InitializeSplat();
			resolver.InitializeReactiveUI();

			services.AddSingleton<Global>();

			services.AddSingleton<QRCodeGenerator>();
			services.AddSingleton<IndexViewModel>();
			services.AddSingleton<SendViewModel>();
			services.AddTransient<LoadWalletViewModel>();
			services.AddTransient<WalletInfoViewModel>();
			services.AddTransient<NewPasswordViewModel>();


			return services;
		}
	}
}
