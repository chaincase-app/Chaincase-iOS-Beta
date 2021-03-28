using System;
using System.IO;
using Chaincase;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace Chaincase.UI.ViewModels
{
	public class LoadWalletViewModel : ReactiveObject
	{
		protected Global Global { get; }
		private Config Config { get; }
		private UiConfig UiConfig { get; }
		protected IHsmStorage Hsm { get; }

		private string _password;
		private string _seedWords;

		private readonly string ACCOUNT_KEY_PATH = $"m/{KeyManager.DefaultAccountKeyPath}";
		private const int MIN_GAP_LIMIT = KeyManager.AbsoluteMinGapLimit * 4;

		public LoadWalletViewModel(Global global, Config config, UiConfig uiConfig, IHsmStorage hsmStorage)
		{
			Global = global;
			Config = config;
			UiConfig = uiConfig;
			Hsm = hsmStorage;
		}

		public bool LoadWallet()
		{
			SeedWords = Guard.Correct(SeedWords);
			Password = Guard.Correct(Password); // Do not let whitespaces to the beginning and to the end.

			string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Config.Network}.json");
			bool isLoadSuccessful;

			try
			{
				KeyPath.TryParse(ACCOUNT_KEY_PATH, out KeyPath keyPath);

				var mnemonic = new Mnemonic(SeedWords);
				var km = KeyManager.Recover(mnemonic, Password, filePath: null, keyPath, MIN_GAP_LIMIT);
				km.SetNetwork(Config.Network);
				km.SetFilePath(walletFilePath);
				Global.WalletManager.AddWallet(km);
				Hsm.SetAsync($"{Config.Network}-seedWords", SeedWords.ToString()); // PROMPT
				UiConfig.HasSeed = true;
				UiConfig.ToFile();
				isLoadSuccessful = true;
			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
				isLoadSuccessful =  false;
			}
			return isLoadSuccessful;
		}

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
