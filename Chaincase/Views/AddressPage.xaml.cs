using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Chaincase.Views
{
	public partial class AddressPage : ReactiveContentPage<AddressViewModel>
	{

		public AddressPage()
		{
			InitializeComponent();

			this.WhenActivated(d =>
			{
				this.Bind(ViewModel,
					vm => vm.Memo,
					v => v.Memo.Text)
					.DisposeWith(d);
				this.Bind(ViewModel,
					vm => vm.Address,
					v => v.Address.Text)
					.DisposeWith(d);
				this.BindCommand(ViewModel,
					vm => vm.ShareCommand,
					v => v.ShareButton,
                    vm => vm.Address)
					.DisposeWith(d);
			});
		}
	}
}
