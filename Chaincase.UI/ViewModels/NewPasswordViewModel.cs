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
using Chaincase.Common.Models;

namespace Chaincase.UI.ViewModels
{
    public class NewPasswordViewModel : ReactiveObject
    {
        private readonly ChaincaseWalletManager _walletManager;
        private readonly Config _config;
        private readonly UiConfig _uiConfig;
        private readonly SensitiveStorage _storage;
        private readonly BitcoinStore _bitcoinStore;
        private readonly ChaincaseSynchronizer _synchronizer;
        private readonly ChaincaseClient _chaincaseClient;

        public const int PasswordMinLength = 8;

        [Required]
        [MinLength(PasswordMinLength, ErrorMessage = "Make it 8 or more characters")]
        public string Password { get; set; }

        public NewPasswordViewModel(ChaincaseWalletManager walletManager, Config config, UiConfig uiConfig, SensitiveStorage storage, BitcoinStore bitcoinStore, ChaincaseSynchronizer sync, ChaincaseClient chaincaseClient)
        {
            _walletManager = walletManager;
            _config = config;
            _uiConfig = uiConfig;
            _storage = storage;
            _bitcoinStore = bitcoinStore;
            _synchronizer = sync;
            _chaincaseClient = chaincaseClient;
        }

        public async Task InitNewWallet(string password)
        {
            Mnemonic seedWords = null;
            LatestMatureHeaderResponse res = null;
            try
            {
	            res = await _chaincaseClient.GetLatestMatureHeader();
	            var header = new SmartHeader(res.MatureHeaderHash, res.MatureHeaderPrevHash, res.MatureHeight, res.MatureHeaderTime);
	           
	            //if only a hundred blocks from sync, keep the sync going instead 
	            //note: on regtest, 101 blocks is what you would most likely have. 100 is good to test this sync from height feature.
	             if ((_bitcoinStore.IndexStore.SmartHeaderChain.TipHeight - res.MatureHeight) > _synchronizer.GetMaxFilterFetch())
	            {
		            await _synchronizer.StopAsync();
		            await _bitcoinStore.IndexStore.ResetFromHeaderAsync(header);
		           _synchronizer.Restart();
	            }
            }
            catch (Exception e)
            {
	            //this endpoint may not be available depending on which version of it is running.
            }
            
            await Task.Run(async () =>
            {
                // Here we are not letting anything that will be autocorrected later.
                // Generate wallet with password exactly as entered for compatibility.
                PasswordHelper.Guard(password);

                string walletFilePath = Path.Combine(_walletManager.WalletDirectories.WalletsDir, $"{_config.Network}.json");
                _ =  KeyManager.CreateNew(out seedWords, password, walletFilePath);
                
                _walletManager.CurrentWallet = _walletManager.GetWalletByName(_config.Network.ToString(), true);
            });

            await _storage.SetSeedWords(password, seedWords.ToString());

            // this should not be a config
            _uiConfig.HasSeed = true;
            _uiConfig.ToFile();
        }
    }
}
