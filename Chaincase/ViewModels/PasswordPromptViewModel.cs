using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Common;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase.ViewModels
{
	public class PasswordPromptViewModel : ViewModelBase
	{
		protected Global Global { get; }

		private string _password;
		private string _headingText;
		private string _acceptText;

		public PasswordPromptViewModel(string acceptText = "ACCEPT", string headingText = "Confirm Send")
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();
			Password = "";
			HeadingText = headingText;
			ValidatePasswordCommand = ReactiveCommand.CreateFromObservable(ValidatePassword);

			CancelCommand = ReactiveCommand.CreateFromObservable(ViewStackService.PopModal);
			_acceptText = acceptText;
		}

        // subscribe to this function after this model is made from within
        // the calling/pushing viewmodel
        public IObservable<string> ValidatePassword()
        {
			return Observable.Start(() =>
			{
				string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Global.Network}.json");
				ExtKey keyOnDisk;
				try
				{
					keyOnDisk = KeyManager.FromFile(walletFilePath).GetMasterExtKey(Password);
				}
				catch
				{
					// bad password
					return null;
				}
				return Password;
			});
        }

		public ReactiveCommand<Unit, string> ValidatePasswordCommand;
		public ReactiveCommand<Unit, Unit> CancelCommand;

		public string AcceptText
		{
			get => _acceptText;
			set => this.RaiseAndSetIfChanged(ref _acceptText, value);
		}

		public string HeadingText
		{
			get => _headingText;
			set => this.RaiseAndSetIfChanged(ref _headingText, value);
		}

		public string Password
        {
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
        }
	}
}
