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
	public class SendWhoViewModel : ViewModelBase
	{
		private string _password;
		private string _address;
		private bool _isBusy;
		private string _memo;
		private CoinListViewModel _coinList;
		private string _warning;
		private SendAmountViewModel _sendAmountViewModel;

		public SendWhoViewModel(IScreen hostScreen, SendAmountViewModel savm) : base(hostScreen)
		{
			BuildTransactionCommand = ReactiveCommand.CreateFromTask(async () =>
			{
				try
				{
					IsBusy = true;
					Password = Guard.Correct(Password);
					Memo = Memo.Trim(',', ' ').Trim();

					var selectedCoinViewModels = savm.CoinList.Coins.Where(cvm => cvm.IsSelected);
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
					if (!Money.TryParse(savm.AmountText, out amount) || amount == Money.Zero)
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
					Logger.LogDebug<SendWhoViewModel>(ex);
					//SetWarningMessage($"Not enough coins selected. You need an estimated {needed.ToString(false, true)} BTC more to make this transaction.");
				}
				catch (Exception ex)
				{
					Logger.LogDebug<SendWhoViewModel>(ex);
					//SetWarningMessage(ex.ToTypeMessageString());
				}
				finally
				{
					IsBusy = false;
				}
			});

            NavigateBack = HostScreen.Router.NavigateBack;
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

		public string Memo
		{
			get => _memo;
			set => this.RaiseAndSetIfChanged(ref _memo, value);
		}

		public ReactiveCommand<Unit, Unit> BuildTransactionCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateBack;

    }
}
