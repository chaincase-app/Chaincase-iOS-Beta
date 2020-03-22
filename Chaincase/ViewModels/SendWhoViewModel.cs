using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace Chaincase.ViewModels
{
    public enum Feenum
    {
        Economy,
        Standard,
        Priority
    }

    public class SendWhoViewModel : ViewModelBase
	{
		private string _address;
        private bool _isBusy;
		private string _memo;
        private Feenum _feeChoice;
        private FeeRate _feeRate;
		private Money _estimatedBtcFee;
		private int _feeTarget;
		private int _minimumFeeTarget;
		private int _maximumFeeTarget;
		private CoinListViewModel _coinList;
		private string _warning;
		private SendAmountViewModel _sendAmountViewModel;
		private PasswordPromptViewModel _promptVM;
		protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

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

			FeeChoice = Feenum.Standard; // Default

			Observable.FromEventPattern(SendAmountViewModel.CoinList, nameof(SendAmountViewModel.CoinList.SelectionChanged))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ => SetFees());

			SetFees();
			Observable
				.FromEventPattern<AllFeeEstimate>(Global.FeeProviders, nameof(Global.FeeProviders.AllFeeEstimateChanged))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ =>
				{
					SetFeeTargetLimits();

					if (FeeTarget < MinimumFeeTarget) // Should never happen.
					{
						FeeTarget = MinimumFeeTarget;
					}
					else if (FeeTarget > MaximumFeeTarget)
					{
						FeeTarget = MaximumFeeTarget;
					}

					SetFees();
				})
				.DisposeWith(Disposables);
		}

		private void SetFees()
		{
			AllFeeEstimate allFeeEstimate = Global.FeeProviders?.AllFeeEstimate;

			if (!(allFeeEstimate is null))
			{
				int feeTarget = -1; // blocks: 1 => 10 minutes
                switch (FeeChoice)
                {
					case Feenum.Economy:
						feeTarget = MinimumFeeTarget;
						break;
					case Feenum.Priority:
						feeTarget = MaximumFeeTarget;
						break;
					case Feenum.Standard: // average of the two
                    default:
						feeTarget = 6; // Standard include in 60 minutes
						break;

                }

				int prevKey = allFeeEstimate.Estimations.Keys.First();
				foreach (int target in allFeeEstimate.Estimations.Keys)
				{
					if (feeTarget == target)
					{
						break;
					}
					else if (feeTarget < target)
					{
						feeTarget = prevKey;
						break;
					}
					prevKey = target;
					}
				
				FeeRate = allFeeEstimate.GetFeeRate(feeTarget);

				IEnumerable<SmartCoin> selectedCoins = SendAmountViewModel.CoinList.Coins.Where(cvm => cvm.IsSelected).Select(x => x.Model);

				int vsize = 150;
				if (selectedCoins.Any())
				{
					if (Money.TryParse(SendAmountViewModel.AmountText.TrimStart('~', ' '), out Money amount))
					{
						var inNum = 0;
						var amountSoFar = Money.Zero;
						foreach (SmartCoin coin in selectedCoins.OrderByDescending(x => x.Amount))
						{
							amountSoFar += coin.Amount;
							inNum++;
							if (amountSoFar > amount)
							{
								break;
							}
						}
						vsize = NBitcoinHelpers.CalculateVsizeAssumeSegwit(inNum, 2);
					}
				}

				if (FeeRate != null)
				{
					EstimatedBtcFee = FeeRate.GetTotalFee(vsize);
				}
				else
				{
					// This should not happen. Never.
					// If FeeRate is null we will have problems when building the tx.
					EstimatedBtcFee = Money.Zero;
				}
            }
		}


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

				var selectedInputs = selectedCoinViewModels.Select(c => new TxoRef(c.Model.GetCoin().Outpoint));
				if (FeeRate is null || FeeRate.SatoshiPerByte < 1)
				{
					return false;
				}

				var feeStrategy = FeeStrategy.CreateFromFeeRate(FeeRate);

				var memo = Memo;
				var intent = new PaymentIntent(script, amount, false, memo);

				var result = await Task.Run(() => Global.WalletService.BuildTransaction(
                    password,
                    intent,
                    feeStrategy,
                    allowUnconfirmed: true,
                    allowedInputs: selectedInputs));
				SmartTransaction signedTransaction = result.Transaction;

				await Global.TransactionBroadcaster.SendTransactionAsync(signedTransaction);
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

		private void SetFeeTargetLimits()
		{
			var allFeeEstimate = Global.FeeProviders?.AllFeeEstimate;

			if (allFeeEstimate != null)
			{
				MinimumFeeTarget = allFeeEstimate.Estimations.Min(x => x.Key); // This should be always 2, but bugs will be seen at least if it is not.
				MaximumFeeTarget = allFeeEstimate.Estimations.Max(x => x.Key);
			}
			else
			{
				MinimumFeeTarget = 2;
				MaximumFeeTarget = Constants.SevenDaysConfirmationTarget;
			}
		}

		public FeeRate FeeRate
		{
			get => _feeRate;
			set => this.RaiseAndSetIfChanged(ref _feeRate, value);
		}

        public Feenum FeeChoice
        {
			get => _feeChoice;
			set => this.RaiseAndSetIfChanged(ref _feeChoice, value);
        }

		public Money EstimatedBtcFee
		{
			get => _estimatedBtcFee;
			set => this.RaiseAndSetIfChanged(ref _estimatedBtcFee, value);
		}

		public int FeeTarget
		{
			get => _feeTarget;
			set
			{
				this.RaiseAndSetIfChanged(ref _feeTarget, value);
			}
		}

		public int MinimumFeeTarget
		{
			get => _minimumFeeTarget;
			set => this.RaiseAndSetIfChanged(ref _minimumFeeTarget, value);
		}

		public int MaximumFeeTarget
		{
			get => _maximumFeeTarget;
			set => this.RaiseAndSetIfChanged(ref _maximumFeeTarget, value);
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

		public ReactiveCommand<string, bool> BuildTransactionCommand { get; }
		public ReactiveCommand<Unit, Unit> PromptCommand { get; }
    }
}
