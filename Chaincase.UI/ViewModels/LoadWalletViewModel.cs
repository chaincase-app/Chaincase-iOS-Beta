using System;
using System.IO;
using System.Threading.Tasks;
using Chaincase;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.Wallets;

namespace Chaincase.UI.ViewModels
{
    public class LoadWalletViewModel : ReactiveObject
    {
        private readonly WalletManager _walletManager;
        private readonly IOptions<Config> _config;
        private readonly IOptions<UiConfig> _uiConfig;
        private readonly SensitiveStorage _storage;

        private string _password;
        private string _seedWords;

        private readonly string ACCOUNT_KEY_PATH = $"m/{KeyManager.DefaultAccountKeyPath}";
        private const int MIN_GAP_LIMIT = KeyManager.AbsoluteMinGapLimit * 4;

        public LoadWalletViewModel(ChaincaseWalletManager walletManager, IOptions<Config> config, IOptions<UiConfig> uiConfig, SensitiveStorage storage)
        {
            _walletManager = walletManager;
            _config = config;
            _uiConfig = uiConfig;
            _storage = storage;
        }

        public async Task LoadWallet()
        {
            SeedWords = Guard.Correct(SeedWords);
            Password = Guard.Correct(Password); // Do not let whitespaces to the beginning and to the end.

            string walletFilePath = Path.Combine(_walletManager.WalletDirectories.WalletsDir, $"{_config.Value.Network}.json");

            Mnemonic mnemonic = null;
            await Task.Run(() =>
            {
                KeyPath.TryParse(ACCOUNT_KEY_PATH, out KeyPath keyPath);

                mnemonic = new Mnemonic(SeedWords);
                var km = KeyManager.Recover(mnemonic, Password, filePath: null, keyPath, MIN_GAP_LIMIT);
                km.SetNetwork(_config.Network);
                km.SetFilePath(walletFilePath);
                _walletManager.AddWallet(km);
            });
            await _storage.SetSeedWords(Password, mnemonic.ToString());
            _uiConfig.Value.HasSeed = true;
            _uiConfig.Value.ToFile();
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
