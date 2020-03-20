using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Chaincase.Navigation;
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
	public class SendAmountViewModel : BaseViewModel
	{
		private string _amountText;
		private CoinListViewModel _coinList;

        public ReactiveCommand<Unit, Unit> GoNext;

        public SendAmountViewModel(IViewStackService viewStackService, CoinListViewModel coinList) : base(viewStackService)
        {
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

            GoNext = ReactiveCommand.CreateFromObservable(() =>
            {
                ViewStackService.PushPage(new SendWhoViewModel(viewStackService, this)).Subscribe();
                return Observable.Return(Unit.Default);
            });
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
}
