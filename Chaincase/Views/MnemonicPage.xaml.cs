using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Chaincase.Views
{
	public partial class MnemonicPage : ReactiveContentPage<MnemonicViewModel>
	{

		public MnemonicPage()
		{
			InitializeComponent();
			this.WhenActivated(disposables =>
			{
				this.BindCommand(ViewModel,
					vm => vm.AcceptCommand,
					v => v.Accept)
					.DisposeWith(disposables);
                this.OneWayBind(ViewModel,
					vm => vm.MnemonicString,
					v => v.Mnemonic.Text)
                .DisposeWith(disposables);
            });
		}
	}
}
