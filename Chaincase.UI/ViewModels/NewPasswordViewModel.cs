using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Services;
using Chaincase.Common.Contracts;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Wallets;
using System.ComponentModel.DataAnnotations;
using WalletWasabi.Stores;
using WalletWasabi.Blockchain.Blocks;
using System;

namespace Chaincase.UI.ViewModels
{
    public class NewPasswordViewModel : ReactiveObject
    {
        private readonly WalletManager _walletManager;
        private readonly Config _config;
        private readonly UiConfig _uiConfig;
        private readonly SensitiveStorage _storage;
        private readonly BitcoinStore _bitcoinStore;
        private readonly ChaincaseSynchronizer _synchronizer;

        public const int PasswordMinLength = 8;

        [Required]
        [MinLength(PasswordMinLength, ErrorMessage = "Make it 8 or more characters")]
        public string Password { get; set; }

        public NewPasswordViewModel(ChaincaseWalletManager walletManager, Config config, UiConfig uiConfig, SensitiveStorage storage, BitcoinStore bitcoinStore, ChaincaseSynchronizer sync)
        {
            _walletManager = walletManager;
            _config = config;
            _uiConfig = uiConfig;
            _storage = storage;
            _bitcoinStore = bitcoinStore;
            _synchronizer = sync;
        }

        public async Task SetPasswordAsync(string password)
        {
            Mnemonic seedWords = null;

            ChaincaseClient client = new ChaincaseClient(_config.GetCurrentBackendUri, _config.TorSocks5EndPoint);
            var res = await client.GetLatestMatureHeader();
            var header = new SmartHeader(res.BlockHash, res.PrevHash, (uint)(int)res.Height, res.Time);
            await _synchronizer.StopAsync();
            await _bitcoinStore.IndexStore.ResetFromHeaderAsync(header);
            var requestInterval = TimeSpan.FromSeconds(30);
            if (_config.Network == Network.RegTest)
            {
                requestInterval = TimeSpan.FromSeconds(5);
            }

            int maxFiltSyncCount = _config.Network == Network.Main ? 1000 : 10000; // On testnet, filters are empty, so it's faster to query them together

            _synchronizer.Start(requestInterval, TimeSpan.FromMinutes(5), maxFiltSyncCount);

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
