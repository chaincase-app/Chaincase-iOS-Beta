using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Navigation;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace Chaincase.ViewModels
{
	public class LoadWalletViewModel : ViewModelBase
	{
		protected Global Global { get; }
		protected IHsmStorage Hsm { get; }

		private string _password;
		private string _seedWords;

		private string ACCOUNT_KEY_PATH = $"m/{KeyManager.DefaultAccountKeyPath}";
		private int MIN_GAP_LIMIT = KeyManager.AbsoluteMinGapLimit * 4;

		public LoadWalletViewModel()
            : base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();
			Hsm = Locator.Current.GetService<IHsmStorage>();

			var canLoadWallet = this.WhenAnyValue(x => x.SeedWords, x => x.ACCOUNT_KEY_PATH, x => x.MIN_GAP_LIMIT,
				(seedWords, keyPath, minGapLimit) =>
				{
					return !string.IsNullOrWhiteSpace(seedWords)
						&& !string.IsNullOrWhiteSpace(keyPath)
						&& minGapLimit > KeyManager.AbsoluteMinGapLimit
						&& minGapLimit < 1_000_000;
				});

			LoadWalletCommand = ReactiveCommand.CreateFromObservable<Unit, bool>(_ =>
			{
				SeedWords = Guard.Correct(SeedWords);
				Password = Guard.Correct(Password); // Do not let whitespaces to the beginning and to the end.

				string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Global.Network}.json");

				try
				{
					KeyPath.TryParse(ACCOUNT_KEY_PATH, out KeyPath keyPath);

					var mnemonic = new Mnemonic(SeedWords);
					var km = KeyManager.Recover(mnemonic, Password, filePath: null, keyPath, MIN_GAP_LIMIT);
					km.SetNetwork(Global.Network);
					km.SetFilePath(walletFilePath);
					Global.WalletManager.AddWallet(km);
					Hsm.SetAsync($"{Global.Network}-seedWords", SeedWords.ToString()); // PROMPT
					Global.UiConfig.HasSeed = true;
					Global.UiConfig.ToFile();
				}
				catch (Exception ex)
				{
					Logger.LogError(ex);
					return Observable.Return(false);
				}

				ViewStackService.PushPage(new MainViewModel()).Subscribe();
				return Observable.Return(true);
			}, canLoadWallet);
		}

		public ReactiveCommand<Unit, bool> LoadWalletCommand;

		public string Password
		{
			get => _password;
			set => this.RaiseAndSetIfChanged(ref _password, value);
		}

		public string SeedWords
		{
			get => _seedWords;
			set => this.RaiseAndSetIfChanged(ref _seedWords, value);
		}
	}
}
