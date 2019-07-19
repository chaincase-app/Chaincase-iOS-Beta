using System.Threading.Tasks;
using System.Windows.Input;
using NBitcoin;
using Wasabi.Controllers;
using Wasabi.Navigation;
using Xamarin.Forms;

namespace Wasabi.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
		private Money _balance;
		public Money Balance
		{
			get => _balance;
			set
			{
				_balance = value;
				RaisePropertyChanged(() => _balance);
			}
		}

		public MainViewModel(INavigationService navigationService) : base(navigationService)
		{
			_balance = 0;
		}

		// Need event to watch for balance changes in wallet service

		// need command to switch between this and receive
		public ICommand NavCommand => new Command(async () => await NavigateToReceive());

		private async Task NavigateToReceive()
		{
			await _navigationService.NavigateAsync("ReceivePage");
		}
	}
}
