using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Wallets;

namespace Chaincase.UI.ViewModels
{
    public class NewPasswordViewModel : ReactiveObject
    {
        private readonly WalletManager _walletManager;
        private readonly Config _config;
        private readonly UiConfig _uiConfig;
        private readonly SensitiveStorage _storage;

        public NewPasswordViewModel(WalletManager walletManager, Config config, UiConfig uiConfig, SensitiveStorage storage)
        {
            _walletManager = walletManager;
            _config = config;
            _uiConfig = uiConfig;
            _storage = storage;
        }

        public async Task SetPasswordAsync(string password)
        {
            // Here we are not letting anything that will be autocorrected later.
            // Generate wallet with password exactly as entered for compatibility.
            PasswordHelper.Guard(password);

            string walletFilePath = Path.Combine(_walletManager.WalletDirectories.WalletsDir, $"{_config.Network}.json");
            KeyManager.CreateNew(out Mnemonic seedWords, password, walletFilePath);

            await _storage.SetSeedWords(password, seedWords.ToString());

            // this should not be a config
            _uiConfig.HasSeed = true;
            _uiConfig.ToFile();
        }
    }
}
