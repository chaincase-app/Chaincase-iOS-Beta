using System.IO;
using Chaincase.Common;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase.UI.ViewModels
{
	public class PINViewModel : ReactiveObject
    {
        protected Global Global { get; }

        public PINViewModel(Global global)
        {
            Global = global;
        }

        public bool IsPasswordValid(string password)
        {
            string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Global.Network}.json");
            ExtKey keyOnDisk;
            try
            {
                keyOnDisk = KeyManager.FromFile(walletFilePath).GetMasterExtKey(password ?? "");
            }
            catch
            {
                // bad password
                return false;
            }
            return true;
        }
    }
}
