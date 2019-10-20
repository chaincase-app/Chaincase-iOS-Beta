using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using DynamicData;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.Models;

namespace Chaincase.ViewModels
{
	public class SendViewModel : ViewModelBase
	{
		private string _password;
		private string _address;
		private bool _isBusy;
		private string _label;
		private string _amountText;
		private CoinListViewModel _coinList;

		public string Address
		{
			get => Address;
			set => this.RaiseAndSetIfChanged(ref _address, value);
		}

		public SendViewModel(IScreen hostScreen) : base(hostScreen)
		{
			CoinList = new CoinListViewModel(hostScreen);
			AmountText = "0.0";

			this.WhenAnyValue(x => x.AmountText)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(amount =>
				{
					// Correct amount
					Regex digitsOnly = new Regex(@"[^\d,.]");
					string betterAmount = digitsOnly.Replace(amount, ""); // Make it digits , and . only.

					betterAmount = betterAmount.Replace(',', '.');
					int countBetterAmount = betterAmount.Count(x => x == '.');
					if (countBetterAmount > 1) // Do not enable typing two dots.
					{
						var index = betterAmount.IndexOf('.', betterAmount.IndexOf('.') + 1);
						if (index > 0)
						{
							betterAmount = betterAmount.Substring(0, index);
						}
					}
					var dotIndex = betterAmount.IndexOf('.');
					if (dotIndex != -1 && betterAmount.Length - dotIndex > 8) // Enable max 8 decimals.
					{
						betterAmount = betterAmount.Substring(0, dotIndex + 1 + 8);
					}

					if (betterAmount != amount)
					{
						AmountText = betterAmount;
					}
				});

			BuildTransactionCommand = ReactiveCommand.CreateFromTask(async () =>
			{
				try
				{
					Password = Guard.Correct(Password);
				}
				catch (InsufficientBalanceException ex)
				{
					Money needed = ex.Minimum - ex.Actual;
					Logger.LogDebug<SendViewModel>(ex);
					//SetWarningMessage($"Not enough coins selected. You need an estimated {needed.ToString(false, true)} BTC more to make this transaction.");
				}
				catch (Exception ex)
				{
					Logger.LogDebug<SendViewModel>(ex);
					//SetWarningMessage(ex.ToTypeMessageString());
				}
			},
			this.WhenAny(x => x.AmountText, x => x.Address, x => x.IsBusy,
				(amountText, address, busy) => !string.IsNullOrWhiteSpace(amountText.Value) && !string.IsNullOrWhiteSpace(Address) && !IsBusy));
		}

		public string Password
		{
			get => Password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
		}


		public bool IsBusy
		{
			get => IsBusy;
			set => this.RaiseAndSetIfChanged(ref _isBusy, value);
		}

		public string AmountText
		{
			get => _amountText;
			set => this.RaiseAndSetIfChanged(ref _amountText, value);
		}

		public CoinListViewModel CoinList
		{
			get => _coinList;
			set => this.RaiseAndSetIfChanged(ref _coinList, value);
		}

		public ReactiveCommand<Unit, Unit> BuildTransactionCommand { get; }
	}
}
