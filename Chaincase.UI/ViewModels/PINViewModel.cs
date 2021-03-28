using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase.UI.ViewModels
{
    public class PINViewModel : ReactiveObject
    {
        protected Global Global { get; }
        private readonly Config _config;
        public bool IsBusy { get; set; }

        public PINViewModel(Global global, Config config)
        {
            Global = global;
            _config = config;
        }

        public async Task IsPasswordValidAsync(string password)
        {
            IsBusy = true;
            string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{_config.Network}.json");
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
