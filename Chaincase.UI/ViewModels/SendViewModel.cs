using Chaincase.Common;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;

namespace Chaincase.UI.ViewModels
{
	public class SendViewModel : ReactiveObject
	{
		protected Global Global { get; }

		public SelectCoinsViewModel CoinList;

		private string _label;

		public SendViewModel()
		{
			Global = Locator.Current.GetService<Global>();
		}

		public string Label
		{
			get => _label;
			set => this.RaiseAndSetIfChanged(ref _label, value);
		}
	}
}
