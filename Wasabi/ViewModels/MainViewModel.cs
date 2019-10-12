using System;
using System.Reactive;
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

		private String _balance;
		public String Balance
		{
			get => _balance.ToString();
			set => this.RaiseAndSetIfChanged(ref _balance, value);
		}

		public ReactiveCommand<Unit, Unit> NavReceiveCommand;
		public ReactiveCommand<Unit, Unit> NavSendCommand;

		public MainViewModel(IScreen hostScreen) : base(hostScreen)
		{
			SetBalance();

			if (Disposables != null)
			{
				throw new Exception("Wallet opened before it was closed.");
			}

			Disposables = new CompositeDisposable();

			NavReceiveCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				HostScreen.Router.Navigate.Execute(new ReceiveViewModel(hostScreen)).Subscribe();
				return Observable.Return(Unit.Default);
			});

			NavSendCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				HostScreen.Router.Navigate.Execute(new SendViewModel(hostScreen)).Subscribe();
				return Observable.Return(Unit.Default);
			});

			Observable.FromEventPattern(Global.WalletService.Coins, nameof(Global.WalletService.Coins.CollectionChanged))
				.Merge(Observable.FromEventPattern(Global.WalletService, nameof(Global.WalletService.CoinSpentOrSpenderConfirmed)))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(o => SetBalance())
				.DisposeWith(Disposables);
		}

		private void SetBalance()
		{
			Balance = WalletController.GetBalance().ToString();
		}
	}
}
