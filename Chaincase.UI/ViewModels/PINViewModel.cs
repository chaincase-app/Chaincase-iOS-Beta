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
        public bool IsBusy { get; set; }

        public PINViewModel(Global global)
        {
            Global = global;
        }

        public async Task IsPasswordValidAsync(string password)
        {
            IsBusy = true;
            string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Global.Network}.json");
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
