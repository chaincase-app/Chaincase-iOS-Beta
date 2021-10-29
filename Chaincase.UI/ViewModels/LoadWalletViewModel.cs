using System;
using System.IO;
using System.Threading.Tasks;
using Chaincase;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.BlockFilters;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.Wallets;

namespace Chaincase.UI.ViewModels
{
    public class LoadWalletViewModel : ReactiveObject
    {
        private readonly WalletManager _walletManager;
        private readonly Config _config;
        private readonly UiConfig _uiConfig;
        private readonly SensitiveStorage _storage;
        private readonly ChaincaseBitcoinStore _bitcoinStore;
        private readonly ChaincaseSynchronizer _synchronizer;

        private string _password;
        private string _seedWords;

        private readonly string ACCOUNT_KEY_PATH = $"m/{KeyManager.DefaultAccountKeyPath}";
        private uint? _blockHeight;
        private const int MIN_GAP_LIMIT = KeyManager.AbsoluteMinGapLimit * 4;
        public  readonly Lazy<uint> MinBirthdayWallet;
        public LoadWalletViewModel(ChaincaseWalletManager walletManager, 
	        Config config, 
	        UiConfig uiConfig, 
	        SensitiveStorage storage, 
	        ChaincaseBitcoinStore chaincaseBitcoinStore,
	        ChaincaseSynchronizer synchronizer)
        {
            _walletManager = walletManager;
            _config = config;
            _uiConfig = uiConfig;
            _storage = storage;
            _bitcoinStore = chaincaseBitcoinStore;
            _synchronizer = synchronizer;
            MinBirthdayWallet = new Lazy<uint>(() => StartingFilters.GetStartingFilter(_config.Network).Header.Height);
        }

        public async Task LoadWallet()
        {
            SeedWords = Guard.Correct(SeedWords);
            Password = Guard.Correct(Password); // Do not let whitespaces to the beginning and to the end.

            string walletFilePath = Path.Combine(_walletManager.WalletDirectories.WalletsDir, $"{_config.Network}.json");

            Mnemonic mnemonic = null;
            KeyPath.TryParse(ACCOUNT_KEY_PATH, out KeyPath keyPath);

            mnemonic = new Mnemonic(SeedWords);
            var km = KeyManager.Recover(mnemonic, Password, filePath: null, keyPath, MIN_GAP_LIMIT);
            km.SetNetwork(_config.Network);
            km.SetFilePath(walletFilePath);
                
                
                
            if (BlockHeight != null && _bitcoinStore.IndexStore.StartingHeight < BlockHeight)
            {
	            //no need to do anything, we have all the filters necessary
            }
            else if (BlockHeight != null && _bitcoinStore.IndexStore.StartingHeight < BlockHeight)
            {
	            
	            //we should find a way to fetch the data we need to construct the header at specified block height
	            
	            //ideally, we also just download the difference between current start height and specified block height
	            
	            //for now, we ignore the height and sync from start filter
	            await _synchronizer.StopAsync();
	            await _bitcoinStore.IndexStore.ResetFromHeaderAsync(StartingFilters.GetStartingFilter(_config.Network).Header);
	            _synchronizer.Restart();
            }
            else
            {
	            await _synchronizer.StopAsync();
	            await _bitcoinStore.IndexStore.ResetFromHeaderAsync(StartingFilters.GetStartingFilter(_config.Network).Header);
	            _synchronizer.Restart();
            }
                
            var wallet = _walletManager.AddWallet(km);
            
            await _storage.SetSeedWords(Password, mnemonic.ToString());
            _uiConfig.HasSeed = true;
            _uiConfig.ToFile();
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }
        
        public uint? BlockHeight
        {
	        get => _blockHeight;
	        set => this.RaiseAndSetIfChanged(ref _blockHeight, value);
        }

        public string SeedWords
        {
            get => _seedWords;
            set => this.RaiseAndSetIfChanged(ref _seedWords, value);
        }
    }
}
