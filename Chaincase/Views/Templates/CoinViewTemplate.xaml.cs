using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using Chaincase.Converters;
using NBitcoin;

namespace Chaincase.Views.Templates
{
	public partial class CoinViewTemplate : ReactiveContentView<CoinViewModel>
	{
		public CoinViewTemplate()
		{
			InitializeComponent();

			this.WhenActivated(d =>
			{
                // IsSelected switch bound in XZML
				this.OneWayBind(ViewModel,
								vm => vm.Amount,
								v => v.Amount.Text,
                                AddBitcoinSymbol)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
                    vm => vm.AnonymitySet,
                    v => v.AnonymitySet.Text,
                    ConvertAnonSet)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
                    vm => vm.Clusters,
                    v => v.Clusters.Text)
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.Status,
					v => v.Status.Text,
					vmToViewConverterOverride: new CoinStatusStringConverter())
					.DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.CoinJoinInProgress,
					v => v.DequeueButton.IsVisible)
					.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.DequeueCoin,
					v => v.DequeueButton)
					.DisposeWith(d);
			});
		}

		private string ConvertAnonSet(int anonSet)
        {
			return anonSet >= Config.DefaultPrivacyLevelSome ? "🗽" : "⚠️";
        }

		private string AddBitcoinSymbol(Money bal)
		{
			return "₿ " + bal.ToString();
		}
	}
}
