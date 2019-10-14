using Gma.QrCodeNet.Encoding;
using ReactiveUI;
using System.Threading.Tasks;
using WalletWasabi.KeyManagement;
using Wasabi.Navigation;

namespace Wasabi.ViewModels
{
	public class TestViewModel : ViewModelBase
	{
		private string _label;
		public string Label
		{
			get => _label;
			set => this.RaiseAndSetIfChanged(ref _label, value);
		}

		public TestViewModel(IScreen hostScreen) : base(hostScreen)
		{
			Label = "Test";
		}
	}
}
