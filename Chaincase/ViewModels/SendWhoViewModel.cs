using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
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
        private bool _isBusy;
		private string _memo;
		private SendAmountViewModel _sendAmountViewModel;
		private PasswordPromptViewModel _promptViewModel;
		protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

		public SendWhoViewModel(SendAmountViewModel savm)
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Memo = "";
			Address = "";

			SendAmountViewModel = savm;

			var canPromptPassword = this.WhenAnyValue(x => x.Memo, x => x.Address, x => x.IsBusy,
                (memo, addr, isBusy) => {
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
				return !isBusy && memo.Length > 0 && address is BitcoinAddress;
                });

			_promptViewModel = new PasswordPromptViewModel("Send 📤");
			_promptViewModel.ValidatePasswordCommand.Subscribe(async validPassword =>
			{
                if (validPassword != null)
                {
					await ViewStackService.PopModal();
					await BuildTransaction(validPassword);
					await ViewStackService.PushPage(new SentViewModel());
                }
			});
            PromptCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				ViewStackService.PushModal(_promptViewModel).Subscribe();
				return Observable.Return(Unit.Default);
            }, canPromptPassword);
		}

		public async Task<bool> BuildTransaction(string password)
		{
			try
			{
				IsBusy = true;
				password = Guard.Correct(password);
				Memo = Memo.Trim(',', ' ').Trim();

				var selectedCoinViewModels = SendAmountViewModel.CoinList.Coins.Where(cvm => cvm.IsSelected);
				var selectedCoinReferences = selectedCoinViewModels.Select(cvm => cvm.Model.OutPoint).ToList();
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

				if (SendAmountViewModel.FeeRate is null || SendAmountViewModel.FeeRate.SatoshiPerByte < 1)
				{
					return false;
				}

				var feeStrategy = FeeStrategy.CreateFromFeeRate(SendAmountViewModel.FeeRate);

				var memo = Memo;
				var intent = new PaymentIntent(script, amount, false, memo);

				var result = await Task.Run(() => Global.Wallet.BuildTransaction(
					password,
					intent,
					feeStrategy,
					allowUnconfirmed: true,
					allowedInputs: selectedCoinReferences));
				SmartTransaction signedTransaction = result.Transaction;

				await Global.TransactionBroadcaster.SendTransactionAsync(signedTransaction); // put this on non-ui theread?
				return true; // seems not to get here
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

        public SendAmountViewModel SendAmountViewModel
        {
			get => _sendAmountViewModel;
			set => this.RaiseAndSetIfChanged(ref _sendAmountViewModel, value);
        }

		public ReactiveCommand<Unit, Unit> PromptCommand { get; }
    }
}
