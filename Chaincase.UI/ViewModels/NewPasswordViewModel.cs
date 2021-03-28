using System.IO;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;

namespace Chaincase.UI.ViewModels
{
	public class NewPasswordViewModel : ReactiveObject
	{
		protected Global Global { get; }
		private Config Config { get; }
		private UiConfig UiConfig { get; }
		protected IHsmStorage Hsm { get; }

		public NewPasswordViewModel(Global global, Config config, UiConfig uiConfig, IHsmStorage hsmStorage)
		{
			Global = global;
			Config = config;
			UiConfig = uiConfig;
			Hsm = hsmStorage;
		}

		public void SetPassword(string password)
		{
			// Here we are not letting anything that will be autocorrected later.
			// Generate wallet with password exactly as entered for compatibility.
			// Todo what do we do if PasswordHelper.Guard fails?
			PasswordHelper.Guard(password);

			string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Config.Network}.json");
			KeyManager.CreateNew(out Mnemonic seedWords, password, walletFilePath);
			// MUST prompt permissions
			Hsm.SetAsync($"{Config.Network}-seedWords", seedWords.ToString());

			UiConfig.HasSeed = true;
			UiConfig.ToFile();
		}
	}
}
