using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Chaincase.Views
{
	public partial class ReceivePage : ReactiveContentPage<ReceiveViewModel>
	{
		public ReceivePage()
		{
			InitializeComponent();
            this.WhenActivated(disposables =>
            {
                this.BindCommand(ViewModel,
                    vm => vm.GenerateCommand,
                    v => v.Generate)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
					vm => vm.Memo,
					v => v.Memo.Text)
                .DisposeWith(disposables);
            });
        }
	}
}
