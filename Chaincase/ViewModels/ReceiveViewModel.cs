using System;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Navigation;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;
using Xamarin.Forms;

namespace Chaincase.ViewModels
{
    public class ReceiveViewModel : ViewModelBase
	{
		private string _memo = "";
		public string Memo
		{
			get => _memo;
			set => this.RaiseAndSetIfChanged(ref _memo, value);
		}

		public ReactiveCommand<Unit, Unit> GenerateCommand { get; }

		public ReceiveViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			GenerateCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				Device.BeginInvokeOnMainThread(() =>
				{
                    HdPubKey toReceive = Global.WalletService.KeyManager.GetNextReceiveKey(Memo, out bool minGapLimitIncreased);
                    if (minGapLimitIncreased)
                    {
                        int minGapLimit = Global.WalletService.KeyManager.MinGapLimit.Value;
                        int prevMinGapLimit = minGapLimit - 1;
                    }
                    Memo = null;
					ViewStackService.PushPage(new AddressViewModel(toReceive)).Subscribe();
				});
				return Observable.Return(Unit.Default);
			}, this.WhenAnyValue(vm => vm.Memo, memo => memo.Length > 0));
		}
	}
}
