using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Chaincase.Common;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Helpers;
using Chaincase.UI.Services;
using NBitcoin.Payment;

namespace Chaincase.UI.ViewModels
{
    public class SendViewModel : ReactiveObject
    {
        protected Global Global { get; }

        private bool _isMax;
        private string _amountText;
        readonly ObservableAsPropertyHelper<string> _sendFromText;
        private FeeRate _feeRate;
        private Money _allSelectedAmount;
        private Money _estimatedBtcFee;
        private int _feeTarget;
        private int _minimumFeeTarget;
        private int _maximumFeeTarget;
        private ObservableAsPropertyHelper<bool> _minMaxFeeTargetsEqual;
        private readonly ObservableAsPropertyHelper<bool> _isTransactionOkToSign;

        private readonly ObservableAsPropertyHelper<BitcoinAddress> _address;
        private readonly ObservableAsPropertyHelper<Money> _outputAmount;
        private string _destinationString;
        private bool _isBusy;
        private string _label;
        private SelectCoinsViewModel _selectCoinsViewModel;

        protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

        public SendViewModel(Global global)
        {
	        Global = global;
            SelectCoinsViewModel = new SelectCoinsViewModel(global);
            AmountText = "0.0";
            AllSelectedAmount = Money.Zero;
            EstimatedBtcFee = Money.Zero;

            this.WhenAnyValue(x => x.AmountText)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(amount =>
                {
                    // Correct amount
                    if (IsMax)
                    {
                        SetAmountIfMax();
                    }
                    else
                    {
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
                    }
                });

            _outputAmount = this.WhenAnyValue(x => x.AmountText,
                (amountText) =>
                {
                    Money.TryParse(amountText.TrimStart('~', ' '), out Money outputAmount);
                    return outputAmount;
                }).ToProperty(this, x => x.OutputAmount);


            this.WhenAnyValue(x => x.IsMax)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isMax =>
                {
                    if (isMax)
                        SetAmountIfMax();
                });

            Observable.FromEventPattern(SelectCoinsViewModel, nameof(SelectCoinsViewModel.SelectionChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => SetFees());

            _minMaxFeeTargetsEqual = this.WhenAnyValue(x => x.MinimumFeeTarget, x => x.MaximumFeeTarget, (x, y) => x == y)
                .ToProperty(this, x => x.MinMaxFeeTargetsEqual, scheduler: RxApp.MainThreadScheduler);

            SetFeeTargetLimits();
            FeeTarget = Global.UiConfig.FeeTarget;
            FeeRate = new FeeRate((decimal)50); //50 sat/vByte placeholder til loads
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

           

            this.WhenAnyValue(x => x.FeeTarget)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    SetFees();
                });

            _address = this.WhenAnyValue(x => x.DestinationString,
                (string destinationString) =>
                {
                    if (destinationString == null) return null;
                    BitcoinAddress address;
                    try
                    {
                        address = BitcoinAddress.Create(destinationString.Trim(), Global.Network);
                        return address;
                    }
                    catch (Exception) { /* not a straight address */ }

                    try
                    {
                        address = new BitcoinUrlBuilder(destinationString, Global.Network).Address;
                        return address;
                    }
                    catch (Exception) { /* not a bitcoin: uri */ }
                    address = null;
                    return address;
                }).ToProperty(this, x => x.Address);



			_isTransactionOkToSign = this.WhenAnyValue(x => x.Label, x => x.Address, x => x.OutputAmount,
				(label, address, ouputAmount) =>
				{
                    //BitcoinAddress address;
                    //try
                    //{
                    //	address = BitcoinAddress.Create(addr.Trim(), Global.Network);
                    //}
                    //catch (FormatException)
                    //{
                    //	// SetWarningMessage("Invalid address.");
                    //	return false;
                    //}

                    //Money amountToSend;
                    //try
                    //{
                    //	Money.TryParse(amountToSendText, out amountToSend);
                    //}
                    //catch (Exception)
                    //{
                    //	return false;
                    //}

                    return Label.NotNullAndNotEmpty()
                        && address is not null
                        && ouputAmount > Money.Zero;
					//return amountToSend > Money.Zero
					//		&& label.NotNullAndNotEmpty()
					//		&& address is BitcoinAddress
					//		&& amountToSend <= selectedAmount.SelectedAmount;
					//return !addr.IsNullOrWhiteSpace();
				}).ToProperty(this, x => x.IsTransactionOkToSign);

			//_promptViewModel = new PasswordPromptViewModel("SEND");
			//_promptViewModel.ValidatePasswordCommand.Subscribe(async validPassword =>
			//{
			//    if (validPassword != null)
			//    {
			//        await ViewStackService.PopModal();
			//        await BuildTransaction(validPassword);
			//        await ViewStackService.PushPage(new SentViewModel());
			//    }
			//});
			//PromptCommand = ReactiveCommand.CreateFromObservable(() =>
			//{
			//    ViewStackService.PushModal(_promptViewModel).Subscribe();
			//    return Observable.Return(Unit.Default);
			//}, canPromptPassword);

		}

        private void SetFees()
        {
            AllFeeEstimate allFeeEstimate = Global.FeeProviders?.AllFeeEstimate;

            if (allFeeEstimate is { })
            {
                int feeTarget = FeeTarget;

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

                IEnumerable<SmartCoin> selectedCoins = SelectCoinsViewModel.CoinList.Where(cvm => cvm.IsSelected).Select(x => x.Model);

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
                long all = selectedCoins.Sum(x => x.Amount);
                AllSelectedAmount = Math.Max(Money.Zero, all - EstimatedBtcFee);
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

        private void SetAmountIfMax()
        {
            if (IsMax)
            {
                AmountText = AllSelectedAmount == Money.Zero
                    ? EstimatedBtcFee >= AllSelectedAmount
                        ? "Too high fee"
                        : "No Coins Selected"
                    : $"~ {AllSelectedAmount.ToString(false, true)}";
            }
        }

        private bool AmountTextPositive(string amountText)
        {
            try
            {
                var amount = Money.Zero;
                Money.TryParse(AmountText.TrimStart('~', ' '), out amount);
                return amount > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public BitcoinAddress Address => _address.Value;

        public Money OutputAmount => _outputAmount.Value;

        public bool IsTransactionOkToSign => _isTransactionOkToSign.Value;

        public bool IsMax
        {
            get => _isMax;
            set => this.RaiseAndSetIfChanged(ref _isMax, value);
        }

        public string AmountText
        {
            get => _amountText;
            set => this.RaiseAndSetIfChanged(ref _amountText, value);
        }
        public FeeRate FeeRate
        {
            get => _feeRate;
            set => this.RaiseAndSetIfChanged(ref _feeRate, value);
        }

        public Money AllSelectedAmount
        {
            get => _allSelectedAmount;
            set => this.RaiseAndSetIfChanged(ref _allSelectedAmount, value);
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

        public bool MinMaxFeeTargetsEqual => _minMaxFeeTargetsEqual.Value;

        public SelectCoinsViewModel SelectCoinsViewModel
        {
            get => _selectCoinsViewModel;
            set => this.RaiseAndSetIfChanged(ref _selectCoinsViewModel, value);
        }

        public string SendFromText => _sendFromText.Value;

        public string DestinationString
        {
            get => _destinationString;
            set => this.RaiseAndSetIfChanged(ref _destinationString, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        public string Label
        {
            get => _label;
            set => this.RaiseAndSetIfChanged(ref _label, value);
        }
    }
}
