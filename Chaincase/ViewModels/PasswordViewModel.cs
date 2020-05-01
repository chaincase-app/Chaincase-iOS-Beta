using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System;
using Chaincase.Navigation;
using Splat;
using NBitcoin;
using WalletWasabi.Blockchain.Keys;
using System.IO;

namespace Chaincase.ViewModels
{
	public class PasswordViewModel : ViewModelBase
	{
		protected Global Global { get; }

		private string _password;

		public PasswordViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();
			SubmitCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				var mnemonic = GenerateMnemonic(Password, Global.Network).ToString();
				ViewStackService.PushPage(new MnemonicViewModel(mnemonic)).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}

		private Mnemonic GenerateMnemonic(string passphrase, NBitcoin.Network network)
		{
			string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{network}.json");
			KeyManager.CreateNew(out Mnemonic mnemonic, passphrase, walletFilePath);
			return mnemonic;
		}

		public ReactiveCommand<Unit, Unit> SubmitCommand;
		public string Password
		{
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
		}
	}
}
