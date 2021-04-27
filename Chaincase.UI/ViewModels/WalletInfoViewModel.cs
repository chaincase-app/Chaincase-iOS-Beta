using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Microsoft.Extensions.Options;
using ReactiveUI;

namespace Chaincase.UI.ViewModels
{
    public class WalletInfoViewModel : ReactiveObject
    {
        private readonly ChaincaseWalletManager _walletManager;
        private readonly IOptions<Config> _config;
        private readonly IOptions<UiConfig> _uiConfig;
        private readonly IShare _share;
        private readonly IDataDirProvider _dataDirProvider;

        public bool HasNoSeedWords => !_uiConfig.HasSeed && !_uiConfig.HasIntermediateKey;

        public WalletInfoViewModel(ChaincaseWalletManager walletManager, IOptions<Config> config, IOptions<UiConfig> uiConfig, IShare share, IDataDirProvider dataDirProvider)
        {
            _walletManager = walletManager;
            _config = config;
            _uiConfig = uiConfig;
            _share = share;
            _dataDirProvider = dataDirProvider;
        }

        public async Task ShareDebugLog()
        {
            var file = Path.Combine(_dataDirProvider.Get(), "Logs.txt");

            await _share.ShareFile(file, "Share Debug Logs");
        }

        public async Task ExportWallet()
        {
            var file = Path.Combine(_dataDirProvider.Get(), $"Wallets/{_config.Value.Network}.json");

            await _share.ShareFile(file, "Export Wallet");
        }

        public string ExtendedAccountPublicKey => _walletManager.CurrentWallet.KeyManager.ExtPubKey.ToString(_config.Value.Network) ?? "";
        public string AccountKeyPath => $"m/{ _walletManager.CurrentWallet.KeyManager.AccountKeyPath}";
    }
}
