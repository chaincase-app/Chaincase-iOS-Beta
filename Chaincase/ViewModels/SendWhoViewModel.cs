using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace Chaincase.ViewModels
{
    public class SendWhoViewModel : BaseViewModel
	{
		private string _password;
		private string _address;
        private FeeRate _feeRate;
        private bool _isBusy;
		private string _memo;
		private CoinListViewModel _coinList;
		private string _warning;
		private SendAmountViewModel _sendAmountViewModel;

		public SendWhoViewModel(IViewStackService viewStackService, SendAmountViewModel savm) : base(viewStackService)
		{
			SendAmountViewModel = savm;
			BuildTransactionCommand = ReactiveCommand.CreateFromTask<string>(BuildTransaction);
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

        public FeeRate FeeRate
        {
            get => _feeRate;
            set => this.RaiseAndSetIfChanged(ref _feeRate, value);
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

        public SendAmountViewModel SendAmountViewModel
        {
			get => _sendAmountViewModel;
			set => this.RaiseAndSetIfChanged(ref _sendAmountViewModel, value);
        }

		public ReactiveCommand<string, Unit> BuildTransactionCommand { get; }

        public async Task BuildTransaction(string password)
        {
			try
			{
				IsBusy = true;
				Password = Guard.Correct(password);
				Memo = Memo.Trim(',', ' ').Trim();

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
				if (!Money.TryParse(SendAmountViewModel.AmountText, out amount) || amount == Money.Zero)
				{
					// SetWarningMessage($"Invalid amount.");
					return;
				}

				var selectedInputs = Global.WalletService.Coins.Select(c => new TxoRef(c.GetCoin().Outpoint));
				// This gives us a suggestion
				var feeEstimate = Global.FeeProviders.AllFeeEstimate;
                var feeTarget = feeEstimate.Estimations.Max(x => x.Key);
                var feeStrategy = FeeStrategy.CreateFromFeeRate(new FeeRate((Money) feeTarget));

				var memo = Memo;
				var intent = new PaymentIntent(script, amount, false, memo);

				var result = await Task.Run(() => Global.WalletService.BuildTransaction(Password, intent, feeStrategy, allowUnconfirmed: true, allowedInputs: selectedInputs));
				SmartTransaction signedTransaction = result.Transaction;

				await Task.Run(async () => await Global.TransactionBroadcaster.SendTransactionAsync(signedTransaction));
			}
			catch (InsufficientBalanceException ex)
			{
				Money needed = ex.Minimum - ex.Actual;
				Logger.LogDebug(ex);
				//SetWarningMessage($"Not enough coins selected. You need an estimated {needed.ToString(false, true)} BTC more to make this transaction.");
			}
			catch (Exception ex)
			{
				Logger.LogDebug(ex);
				//SetWarningMessage(ex.ToTypeMessageString());
			}
			finally
			{
				IsBusy = false;
			}
		}
    }
}
