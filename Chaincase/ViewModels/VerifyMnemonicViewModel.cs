using System;
using Chaincase.Navigation;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using Splat;
using NBitcoin;
using WalletWasabi.Blockchain.Keys;
using System.IO;

namespace Chaincase.ViewModels
{
	public class VerifyMnemonicViewModel : ViewModelBase
	{
		protected Global Global { get; }

		private string _mnemonicString { get; }
		private string[] _mnemonicWords { get; }
		private string _passphrase;
		private bool _triedVerifyWithoutChange;
		private string _recall3;
		private string _recall2;
		private string _recall1;
		private string _recall0;

		public VerifyMnemonicViewModel(string mnemonicString)
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();

			_mnemonicString = mnemonicString;
			_mnemonicWords = mnemonicString.Split(" ");
			Recall0 = Recall1 = Recall2 = Recall3 = "";

			VerifyCommand = ReactiveCommand.CreateFromObservable(Verify);
			VerifyCommand.Subscribe(verified =>
			{
				if (verified)
                {
					App.LoadWalletAsync();
				    ViewStackService.PushPage(new MainViewModel()).Subscribe();
                }
			});
		}

        public IObservable<bool> Verify()
        {
			return Observable.Start(() =>
			{
				return string.Equals(Recall0, _mnemonicWords[0], StringComparison.CurrentCultureIgnoreCase) &&
					 string.Equals(Recall1, _mnemonicWords[3], StringComparison.CurrentCultureIgnoreCase) &&
					 string.Equals(Recall2, _mnemonicWords[6], StringComparison.CurrentCultureIgnoreCase) &&
					 string.Equals(Recall3, _mnemonicWords[9], StringComparison.CurrentCultureIgnoreCase) &&
					 VerifyWalletCredentials(_mnemonicString, _passphrase);
			});
		}

		private bool VerifyWalletCredentials(string mnemonicString, string passphrase)
		{
			Mnemonic mnemonic = new Mnemonic(mnemonicString);
			ExtKey derivedExtKey = mnemonic.DeriveExtKey(passphrase);

			string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Global.Network}.json");
			ExtKey keyOnDisk;
			try
			{
				keyOnDisk = KeyManager.FromFile(walletFilePath).GetMasterExtKey(passphrase);
			}
			catch
			{
				// bad password
				return false;
			}
			return keyOnDisk.Equals(derivedExtKey);
		}

		public ReactiveCommand<Unit, bool> VerifyCommand;

		public string Recall0
		{
			get => _recall0;
			set => this.RaiseAndSetIfChanged(ref _recall0, value);
		}

		public string Recall1
		{
			get => _recall1;
			set => this.RaiseAndSetIfChanged(ref _recall1, value);
		}
		public string Recall2
		{
			get => _recall2;
			set => this.RaiseAndSetIfChanged(ref _recall2, value);
		}
		public string Recall3
		{
			get => _recall3;
			set => this.RaiseAndSetIfChanged(ref _recall3, value);
		}

		public bool TriedVerifyWithoutChange
		{
			get => _triedVerifyWithoutChange;
			set => this.RaiseAndSetIfChanged(ref _triedVerifyWithoutChange, value);
		}

		public string Passphrase
		{
			get => _passphrase;
			set => this.RaiseAndSetIfChanged(ref _passphrase, value);
		}
	}
}
