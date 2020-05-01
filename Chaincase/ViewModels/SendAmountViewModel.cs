using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Helpers;

namespace Chaincase.ViewModels
{
	public class SendAmountViewModel : ViewModelBase
	{
		protected Global Global { get; }

		private string _amountText;
		private CoinListViewModel _coinList;
		private Feenum _feeChoice;
		private FeeRate _feeRate;
		private Money _estimatedBtcFee;
		private int _feeTarget;
		private int _minimumFeeTarget;
		private int _maximumFeeTarget;

		public ReactiveCommand<Unit, Unit> GoNext;

        protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

        public SendAmountViewModel(CoinListViewModel coinList)
            : base(Locator.Current.GetService<IViewStackService>())
        {
			Global = Locator.Current.GetService<Global>();
            CoinList = coinList;
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

			FeeChoice = Feenum.Standard; // Default

			Observable.FromEventPattern(CoinList, nameof(CoinList.SelectionChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => SetFees());

			this.WhenAnyValue(x => x.FeeChoice).Subscribe(_ => SetFees());

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

            GoNext = ReactiveCommand.CreateFromObservable(() =>
            {
                ViewStackService.PushPage(new SendWhoViewModel(this)).Subscribe();
                return Observable.Return(Unit.Default);
            }, this.WhenAnyValue(
                x => x.AmountText,
                x => x.CoinList.SelectedAmount,
                (amountToSpend, selectedAmount) =>
			    {
				    return AmountTextPositive(amountToSpend) &&
				    Money.Parse(amountToSpend) + EstimatedBtcFee <= selectedAmount;
			    }));
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
						feeTarget = MaximumFeeTarget;
						break;
					case Feenum.Priority:
						feeTarget = MinimumFeeTarget;
						break;
					case Feenum.Standard: // average of the two
					default:
						feeTarget = MaximumFeeTarget < 6 ? MaximumFeeTarget : 6; // Standard include in 60 minutes
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

				FeeTarget = feeTarget;
				FeeRate = allFeeEstimate.GetFeeRate(feeTarget);

				IEnumerable<SmartCoin> selectedCoins = CoinList.Coins.Where(cvm => cvm.IsSelected).Select(x => x.Model);

				int vsize = 150;
				if (selectedCoins.Any())
				{
					if (Money.TryParse(AmountText.TrimStart('~', ' '), out Money amount))
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

		private bool AmountTextPositive(string amountText)
        {
            try {
                var amount = Money.Zero;
                Money.TryParse(amountText, out amount);
                return amount > 0;
            } catch (Exception e)
            {
                return false;
            }
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
	}

	public enum Feenum
	{
		Economy,
		Standard,
		Priority
	}
}
