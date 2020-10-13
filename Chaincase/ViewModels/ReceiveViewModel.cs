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
		protected Global Global { get; }

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
			Global = Locator.Current.GetService<Global>();

			var promptViewModel = new PasswordPromptViewModel("ACCEPT", "Generate Address");
			promptViewModel.ValidatePasswordCommand.Subscribe(async validPassword =>
			{
				if (validPassword != null)
				{
					await ViewStackService.PopModal();
					Device.BeginInvokeOnMainThread(() =>
					{
						HdPubKey toReceive = Global.Wallet.KeyManager.GetNextReceiveKey(Memo, out bool minGapLimitIncreased);
						Memo = "";
						ViewStackService.PushPage(new AddressViewModel(toReceive)).Subscribe();
					});
				}
			});

			GenerateCommand = ReactiveCommand.CreateFromObservable(() =>
			{

				ViewStackService.PushModal(promptViewModel).Subscribe();
				return Observable.Return(Unit.Default);
			}, this.WhenAnyValue(vm => vm.Memo, memo => memo.Length > 0));
		}
	}
}
