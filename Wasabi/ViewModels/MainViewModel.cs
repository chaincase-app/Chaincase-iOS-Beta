using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Models;
using Wasabi.Controllers;
using Wasabi.Navigation;
using Xamarin.Forms;

namespace Wasabi.ViewModels
{
	public class MainViewModel : ViewModelBase
	{

		private CompositeDisposable Disposables { get; set; }

		private Money _balance;
		public Money Balance
		{
			get => _balance;
			set => this.RaiseAndSetIfChanged(ref _balance, value);
		}

		public MainViewModel(INavigationService navigationService) : base(navigationService)
		{
			SetBalance();

			if (Disposables != null)
			{
				throw new Exception("Wallet opened before it was closed.");
			}

			Disposables = new CompositeDisposable();

			Observable.FromEventPattern(Global.WalletService.Coins, nameof(Global.WalletService.Coins.CollectionChanged))
				.Merge(Observable.FromEventPattern(Global.WalletService, nameof(Global.WalletService.CoinSpentOrSpenderConfirmed)))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(o => SetBalance())
				.DisposeWith(Disposables);
		}

		// Need event to watch for balance changes in wallet service

		// need command to switch between this and receive
		public ICommand NavCommand => new Command(async () => await NavigateToReceive());

		public ICommand SendCommand => new Command(() => NavigateToCoinList());

		private async Task NavigateToReceive()
		{
			await _navigationService.NavigateTo(new ReceiveViewModel(_navigationService));
		}

		private async Task NavigateToCoinList()
		{
			await _navigationService.NavigateTo(new CoinListViewModel(_navigationService));
		}

		private void SetBalance()
		{
			Balance = WalletController.GetBalance();
		}
	}
}
