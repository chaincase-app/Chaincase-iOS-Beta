using System.IO;
using NBitcoin;
using WalletWasabi.Wallets;

namespace Chaincase.Common.Services
{
    public class ChaincaseWalletManager : WalletManager
    {
        public Wallet CurrentWallet { get; set; }

        public ChaincaseWalletManager(Network network, WalletDirectories walletDirectories)
            : base(network, walletDirectories)
        {
        }

        public void SetDefaultWallet()
        {
            CurrentWallet = GetWalletByName(Network.ToString());
        }

        public bool HasDefaultWalletFile()
        {
            // this is kinda codesmell biz logic but it doesn't make sense for a full VM here
            var walletName = Network.ToString();
            (string walletFullPath, _) = WalletDirectories.GetWalletFilePaths(walletName);
            return File.Exists(walletFullPath);
        }
    }
}
