using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Services;
using Chaincase.Common.Contracts;
using Microsoft.Extensions.Options;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Wallets;
using System.ComponentModel.DataAnnotations;

namespace Chaincase.UI.ViewModels
{
    public class NewPasswordViewModel : ReactiveObject
    {
        private readonly WalletManager _walletManager;
        private readonly IOptions<Config> _config;
        private readonly IOptions<UiConfig> _uiConfig;
        private readonly SensitiveStorage _storage;

        public const int PasswordMinLength = 8;

        [Required]
        [MinLength(PasswordMinLength, ErrorMessage = "Make it 8 or more characters")]
        public string Password { get; set; }

        public NewPasswordViewModel(ChaincaseWalletManager walletManager, IOptions<Config> config, IOptions<UiConfig> uiConfig, SensitiveStorage storage)
        {
            _walletManager = walletManager;
            _config = config;
            _uiConfig = uiConfig;
            _storage = storage;
        }
        public async Task SetPasswordAsync(string password)
        {
            Mnemonic seedWords = null;
            await Task.Run(() =>
            {
                // Here we are not letting anything that will be autocorrected later.
                // Generate wallet with password exactly as entered for compatibility.
                PasswordHelper.Guard(password);

                string walletFilePath = Path.Combine(_walletManager.WalletDirectories.WalletsDir, $"{_config.Network}.json");
                KeyManager.CreateNew(out seedWords, password, walletFilePath);
            });
            await _storage.SetSeedWords(password, seedWords.ToString());

            // this should not be a config
            _uiConfig.HasSeed = true;
            _uiConfig.ToFile();
        }
    }
}
