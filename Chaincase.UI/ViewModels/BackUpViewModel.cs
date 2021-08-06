using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Security;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Microsoft.Extensions.Options;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Wallets;

namespace Chaincase.UI.ViewModels
{
    public class BackUpViewModel : ReactiveObject
    {
        private readonly IOptions<Config> _config;
        private readonly IOptions<UiConfig> _uiConfig;
        private readonly IHsmStorage _hsm;
        private readonly SensitiveStorage _storage;
        private readonly WalletManager _walletManager;
        private List<string> _seedWords;

        private readonly string ACCOUNT_KEY_PATH = $"m/{KeyManager.DefaultAccountKeyPath}";
        private const int MIN_GAP_LIMIT = KeyManager.AbsoluteMinGapLimit * 4;

        public bool HasNoSeedWords => !_uiConfig.Value.HasSeed && !_uiConfig.Value.HasIntermediateKey;
        public bool IsLegacy => _uiConfig.Value.HasSeed && !_uiConfig.Value.HasIntermediateKey;

        private string LegacyWordsLoc => $"{_config.Value.Network}-seedWords";

        public BackUpViewModel(IOptions<Config> config, IOptions<UiConfig> uiConfig, IHsmStorage hsm, SensitiveStorage storage, ChaincaseWalletManager walletManager)
        {
            _config = config;
            _uiConfig = uiConfig;
            _hsm = hsm;
            _storage = storage;
            _walletManager = walletManager;
        }

        public async Task InitSeedWords(string password)
        {
            string wordString = null;
            KeyManager keyManager = null;
            try
            {
                // ensure correct pw
                PasswordHelper.Guard(password);
                string walletFilePath = Path.Combine(_walletManager.WalletDirectories.WalletsDir, $"{_config.Value.Network}.json");
                await Task.Run(() =>
                {
                    keyManager = KeyManager.FromFile(walletFilePath);
                    keyManager.GetMasterExtKey(password ?? "");
                });

                await Task.Run(async () =>
                {
                    // this next line doesn't really run async :/
                    wordString = await _storage.GetSeedWords(password);
                    if (wordString is null)
                        throw new KeyNotFoundException();
                });
            }
            catch (SecurityException e)
            {
                // toss bad password to UI
                throw e;
            }
            // KeyNotFoundException || ArgumentException
            catch
            {
                // try migrate from the legacy system
                var seedWords = await _hsm.GetAsync(LegacyWordsLoc);
                if (string.IsNullOrEmpty(seedWords))
                {
                    // check if corrupted and show message
                    throw new InvalidOperationException("Try again if you cancelled the biometric authentication. Otherwise, there are no seed words saved. Please back up using \"Export Wallet File\"");
                }

                // check if words match the wallet file
                KeyManager legacyKM = null;
                await Task.Run(() =>
                {
                    KeyPath.TryParse(ACCOUNT_KEY_PATH, out KeyPath keyPath);

                    var mnemonic = new Mnemonic(seedWords);
                    legacyKM = KeyManager.Recover(mnemonic, password, filePath: null, keyPath, MIN_GAP_LIMIT);
                });
                if (legacyKM.EncryptedSecret != keyManager.EncryptedSecret) {
                    throw new InvalidOperationException("Corrupt seed words. Please back up using \"Export Wallet File\" instead.");
                }

                await _storage.SetSeedWords(password, seedWords);
                wordString = seedWords;
            }
            finally
            {
                SeedWords = wordString?.Split(' ').ToList();
            }
        }

        public void SetIsBackedUp()
        {
            _uiConfig.Value.IsBackedUp = true;
            _uiConfig.Value.ToFile(); // successfully backed up!
        }

        public List<string> SeedWords
        {
            get => _seedWords;
            set => this.RaiseAndSetIfChanged(ref _seedWords, value);
        }
    }
}
