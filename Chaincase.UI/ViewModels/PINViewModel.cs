using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Wallets;

namespace Chaincase.UI.ViewModels
{
    public class PINViewModel : ReactiveObject
    {
        private readonly WalletManager _walletManager;
        private readonly Config _config;
        public bool IsBusy { get; set; }

        public PINViewModel(WalletManager walletManager, Config config)
        {
            _walletManager = walletManager;
            _config = config;
        }

        public async Task IsPasswordValidAsync(string password)
        {
            IsBusy = true;
            string walletFilePath = Path.Combine(_walletManager.WalletDirectories.WalletsDir, $"{_config.Network}.json");
            try
            {
                await Task.Run(() => KeyManager.FromFile(walletFilePath).GetMasterExtKey(password ?? ""));
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
