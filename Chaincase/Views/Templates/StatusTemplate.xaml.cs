using ReactiveUI.XamForms;
using Chaincase.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Chaincase.Views.Templates
{
	public partial class StatusTemplate : ReactiveContentView<StatusViewModel>
	{
		public StatusTemplate()
		{
			InitializeComponent();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.Status,
                    v => v.StatusLabel.Text)
                    .DisposeWith(d);
            });
        }
	}
}
