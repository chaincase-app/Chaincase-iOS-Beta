using System.Runtime.InteropServices.WindowsRuntime;
using BTCPayServer.BIP78.Sender;
using NBitcoin;
using WalletWasabi.Blockchain.Keys;

namespace Chainicase.Common.PayJoin.Sender
{
    public class PayjoinWallet : IPayjoinWallet
    {
        private readonly ExtPubKey _extPubKey;
        private readonly RootedKeyPath _rootedKeyPath;

        public PayjoinWallet(ExtPubKey extPubKey, RootedKeyPath rootedKeyPath)
        {
            _extPubKey = extPubKey;
            _rootedKeyPath = rootedKeyPath;
        }

        public IHDScriptPubKey Derive(KeyPath keyPath)
        {
            return ((IHDScriptPubKey)_extPubKey).Derive(keyPath); 
        }
            
        public bool CanDeriveHardenedPath()
        {
            return ((IHDScriptPubKey)_extPubKey).CanDeriveHardenedPath();
        }

        public Script ScriptPubKey => ((IHDScriptPubKey)_extPubKey).ScriptPubKey;

        public ScriptPubKeyType ScriptPubKeyType => ScriptPubKeyType.Segwit;

        public RootedKeyPath RootedKeyPath => _rootedKeyPath;

        public IHDKey AccountKey => _extPubKey;
    }
}