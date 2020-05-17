using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase.ViewModels
{
    public class NewPasswordViewModel : ViewModelBase
	{
		protected Global Global { get; }

		private string _password;

		public NewPasswordViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();
			SubmitCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Global.Network}.json");
				KeyManager.CreateNew(out Mnemonic seedWords, Password, walletFilePath);
                // save seedWords to keychain
                // set/default UiConfig to NotBackedUp
				ViewStackService.PushPage(new MainViewModel()).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		public ReactiveCommand<Unit, Unit> SubmitCommand;
		public string Password
		{
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
		}
	}
}
