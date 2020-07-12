using NBitcoin;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Helpers;
using WalletWasabi.Models;
using Splat;
using Chaincase.Navigation;
using WalletWasabi.Blockchain.Keys;
using Xamarin.Forms;
using Chaincase.ViewModels.Validation;

namespace Chaincase.ViewModels
{
	public class WalletInfoViewModel : ViewModelBase, IDisposable
	{
		protected Global Global { get; }

		private CompositeDisposable Disposables { get; set; }

		private KeyManager _keyManager;
		private bool _showSensitiveKeys;
		private string _password;
		private string _extendedMasterPrivateKey;
		private string _extendedMasterZprv;
		private string _extendedAccountPrivateKey;
		private string _extendedAccountZprv;

		public WalletInfoViewModel(KeyManager keyManager) : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();
			_keyManager = keyManager;

			Disposables = Disposables is null ? new CompositeDisposable() : throw new NotSupportedException($"Cannot open {GetType().Name} before closing it.");

			Closing = new CancellationTokenSource();

			Closing.DisposeWith(Disposables);

			ClearSensitiveData(true);

			// TODO turn bool
			ToggleSensitiveKeysCommand = ReactiveCommand.Create<Unit, bool>(_ =>
			{
				try
				{
					if (ShowSensitiveKeys)
					{
						ClearSensitiveData(true);
					}
					else
					{
						var secret = PasswordHelper.GetMasterExtKey(_keyManager, Password, out string isCompatibilityPasswordUsed);
						Password = "";

						//if (isCompatibilityPasswordUsed != null)
						//{
						//	SetWarningMessage(PasswordHelper.CompatibilityPasswordWarnMessage);
						//}

						string master = secret.GetWif(Global.Network).ToWif();
						string account = secret.Derive(_keyManager.AccountKeyPath).GetWif(Global.Network).ToWif();
						string masterZ = secret.ToZPrv(Global.Network);
						string accountZ = secret.Derive(_keyManager.AccountKeyPath).ToZPrv(Global.Network);
						SetSensitiveData(master, account, masterZ, accountZ);
					}
					return true;
				}
				catch (Exception ex)
				{
					return false;
				}
			});
		}

		private void ClearSensitiveData(bool passwordToo)
		{
			ExtendedMasterPrivateKey = "";
			ExtendedMasterZprv = "";
			ExtendedAccountPrivateKey = "";
			ExtendedAccountZprv = "";
			ShowSensitiveKeys = false;

			if (passwordToo)
			{
				Password = "";
			}
		}

		public CancellationTokenSource Closing { private set; get; }

		public string ExtendedAccountPublicKey => Global.Wallet.KeyManager.ExtPubKey.ToString(Global.Network);
		public string ExtendedAccountZpub => Global.Wallet.KeyManager.ExtPubKey.ToZpub(Global.Network);
		public string AccountKeyPath => $"m/{ Global.Wallet.KeyManager.AccountKeyPath}";
		public string MasterKeyFingerprint => Global.Wallet.KeyManager.MasterFingerprint.ToString();
		public ReactiveCommand<Unit, bool> ToggleSensitiveKeysCommand { get; }

		public bool ShowSensitiveKeys
		{
			get => _showSensitiveKeys;
			set => this.RaiseAndSetIfChanged(ref _showSensitiveKeys, value);
		}

		public ErrorDescriptors ValidatePassword() => PasswordHelper.ValidatePassword(Password);

		[ValidateMethod(nameof(ValidatePassword))]
		public string Password
		{
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
		}

		public string ExtendedMasterPrivateKey
		{
			get => _extendedMasterPrivateKey;
			set => this.RaiseAndSetIfChanged(ref _extendedMasterPrivateKey, value);
		}

		public string ExtendedAccountPrivateKey
		{
			get => _extendedAccountPrivateKey;
			set => this.RaiseAndSetIfChanged(ref _extendedAccountPrivateKey, value);
		}

		public string ExtendedMasterZprv
		{
			get => _extendedMasterZprv;
			set => this.RaiseAndSetIfChanged(ref _extendedMasterZprv, value);
		}

		public string ExtendedAccountZprv
		{
			get => _extendedAccountZprv;
			set => this.RaiseAndSetIfChanged(ref _extendedAccountZprv, value);
		}

		private void SetSensitiveData(string extendedMasterPrivateKey, string extendedAccountPrivateKey, string extendedMasterZprv, string extendedAccountZprv)
		{
			ExtendedMasterPrivateKey = extendedMasterPrivateKey;
			ExtendedAccountPrivateKey = extendedAccountPrivateKey;
			ExtendedMasterZprv = extendedMasterZprv;
			ExtendedAccountZprv = extendedAccountZprv;
			ShowSensitiveKeys = true;

			Device.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await Task.Delay(21000, Closing.Token);
				}
				catch (TaskCanceledException)
				{
					// Ignore
				}
				finally
				{
					ClearSensitiveData(false);
				}
			});
		}

		public void Dispose()
		{
			Closing.Cancel();
			Disposables?.Dispose();
			Disposables = null;

			ClearSensitiveData(true);
		}
	}
}
