using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DynamicData;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.Models;
using WalletWasabi.Services;

namespace Chaincase.ViewModels
{
	public class SendViewModel : ViewModelBase
	{
		private string _password;
		private string _address;
		private bool _isBusy;
		private string _memo;
		private string _amountText;
		private CoinListViewModel _coinList;
		private string _warning;

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
					IsBusy = true;
					Password = Guard.Correct(Password);
					Memo = Memo.Trim(',', ' ').Trim();

					var selectedCoinViewModels = CoinList.Coins.Where(cvm => cvm.IsSelected);
					var selectedCoinReferences = selectedCoinViewModels.Select(cvm => new TxoRef(cvm.Model.TransactionId, cvm.Model.Index)).ToList();
					if (!selectedCoinReferences.Any())
					{
						//SetWarningMessage("No coins are selected to spend.");
						return;
					}

					BitcoinAddress address;
					try
					{
						address = BitcoinAddress.Create(Address.Trim(), Global.Network);
					}
					catch (FormatException)
					{
						// SetWarningMessage("Invalid address.");
						return;
					}

					var script = address.ScriptPubKey;
					var amount = Money.Zero;
					if (!Money.TryParse(AmountText, out amount) || amount == Money.Zero)
					{
						// SetWarningMessage($"Invalid amount.");
						return;
					}

					if (amount == selectedCoinViewModels.Sum(x => x.Amount))
					{
						// SetWarningMessage("Looks like you want to spend a whole coin. Try Max button instead.");
						return;
					}

					var memo = Memo;
					var operation = new WalletService.Operation(script, amount, memo);

					var feeTarget = 500;
					var result = await Task.Run(() => Global.WalletService.BuildTransaction(Password, new[] { operation }, feeTarget, allowUnconfirmed: true, allowedInputs: selectedCoinReferences));
					SmartTransaction signedTransaction = result.Transaction;

					await Task.Run(async () => await Global.WalletService.SendTransactionAsync(signedTransaction));
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
				finally
				{
					IsBusy = false;
				}
			},
			this.WhenAny(x => x.AmountText, x => x.Address, x => x.IsBusy,
				(amountText, address, busy) => !string.IsNullOrWhiteSpace(amountText.Value) && !string.IsNullOrWhiteSpace(address.Value) && !busy.Value)
				.ObserveOn(RxApp.MainThreadScheduler));
		}

		public string Password
		{
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
		}

		public string Address
		{
			get => _address;
			set => this.RaiseAndSetIfChanged(ref _address, value);
		}

		public bool IsBusy
		{
			get => _isBusy;
			set => this.RaiseAndSetIfChanged(ref _isBusy, value);
		}

		public string AmountText
		{
			get => _amountText;
			set => this.RaiseAndSetIfChanged(ref _amountText, value);
		}

		public string Memo
		{
			get => _memo;
			set => this.RaiseAndSetIfChanged(ref _memo, value);
		}

		public CoinListViewModel CoinList
		{
			get => _coinList;
			set => this.RaiseAndSetIfChanged(ref _coinList, value);
		}

		public ReactiveCommand<Unit, Unit> BuildTransactionCommand { get; }
	}
}
