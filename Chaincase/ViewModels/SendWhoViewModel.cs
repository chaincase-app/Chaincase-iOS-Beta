using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace Chaincase.ViewModels
{
    public class SendWhoViewModel : ViewModelBase
	{
		private string _address;
        private FeeRate _feeRate;
        private bool _isBusy;
		private string _memo;
		private CoinListViewModel _coinList;
		private string _warning;
		private SendAmountViewModel _sendAmountViewModel;
		private PasswordPromptViewModel _promptVM;

		public SendWhoViewModel(SendAmountViewModel savm)
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Memo = "";
			Address = "";

			SendAmountViewModel = savm;
			BuildTransactionCommand = ReactiveCommand.CreateFromTask<string, bool>(BuildTransaction);

			var canPromptPassword = this.WhenAnyValue(x => x.Memo, x => x.Address, (memo, addr) => {
				BitcoinAddress address;
				try
				{
					address = BitcoinAddress.Create(addr.Trim(), Global.Network);
				}
				catch (FormatException)
				{
					// SetWarningMessage("Invalid address.");
					return false;
				}
				return memo.Length > 0 && address is BitcoinAddress;
                });
            
			_promptVM = new PasswordPromptViewModel(BuildTransactionCommand);
            PromptCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				ViewStackService.PushModal(_promptVM).Subscribe();
				return Observable.Return(Unit.Default);
            }, canPromptPassword);
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

		public ReactiveCommand<string, bool> BuildTransactionCommand { get; }
		public ReactiveCommand<Unit, Unit> PromptCommand { get; }

		public async Task<bool> BuildTransaction(string password)
        {
			try
			{
				IsBusy = true;
				password = Guard.Correct(password);
				Memo = Memo.Trim(',', ' ').Trim();

				var selectedCoinViewModels = SendAmountViewModel.CoinList.Coins.Where(cvm => cvm.IsSelected);
				var selectedCoinReferences = selectedCoinViewModels.Select(cvm => new TxoRef(cvm.Model.TransactionId, cvm.Model.Index)).ToList();
				if (!selectedCoinReferences.Any())
				{
					//SetWarningMessage("No coins are selected to spend.");
					return false;
				}

				BitcoinAddress address;
				try
				{
					address = BitcoinAddress.Create(Address.Trim(), Global.Network);
				}
				catch (FormatException)
				{
					// SetWarningMessage("Invalid address.");
					return false;
				}

				var script = address.ScriptPubKey;
				var amount = Money.Zero;
				if (!Money.TryParse(SendAmountViewModel.AmountText, out amount) || amount == Money.Zero)
				{
					// SetWarningMessage($"Invalid amount.");
					return false;
				}

				var selectedInputs = Global.WalletService.Coins.Select(c => new TxoRef(c.GetCoin().Outpoint));
				// This gives us a suggestion
				var feeEstimate = Global.FeeProviders.AllFeeEstimate;
                var feeTarget = feeEstimate.Estimations.Max(x => x.Key);
                var feeStrategy = FeeStrategy.CreateFromFeeRate(new FeeRate((Money) feeTarget));

				var memo = Memo;
				var intent = new PaymentIntent(script, amount, false, memo);

				var result = await Task.Run(() => Global.WalletService.BuildTransaction(password, intent, feeStrategy, allowUnconfirmed: true, allowedInputs: selectedInputs));
				SmartTransaction signedTransaction = result.Transaction;

				await Task.Run(async () => await Global.TransactionBroadcaster.SendTransactionAsync(signedTransaction));
				return true;
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
			return false;
		}
    }
}
