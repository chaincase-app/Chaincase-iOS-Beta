using System.ComponentModel.DataAnnotations;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase.UI.ViewModels
{
    public class ReceiveViewModel : ReactiveObject
    {
        private readonly ChaincaseWalletManager _walletManager;
        private readonly Config _config;
        private readonly INotificationManager _notificationManager;

        public Money ProposedAmount { get; private set; }
        private string _proposedLabel;
        private bool[,] _qrCode;
        private string _requestAmount;

        public ReceiveViewModel(ChaincaseWalletManager walletManager, Config config, INotificationManager notificationManager)
        {
            _walletManager = walletManager;
            _config = config;
            _notificationManager = notificationManager;
        }

        public void InitNextReceiveKey()
        {
            ReceivePubKey = _walletManager.CurrentWallet.KeyManager.GetNextReceiveKey(ProposedLabel, out bool minGapLimitIncreased);
            _notificationManager.RequestAuthorization();
        }

        public void UpdateKeyLabel()
        {
	        ReceivePubKey!.SetLabel(ProposedLabel, _walletManager.CurrentWallet.KeyManager);
        }

        public string AppliedLabel => ReceivePubKey?.Label ?? "";
        public string Address => ReceivePubKey?.GetP2wpkhAddress(_config.Network).ToString();
        public string Pubkey => ReceivePubKey?.PubKey.ToString();
        public string KeyPath => ReceivePubKey?.FullKeyPath.ToString();

        public HdPubKey? ReceivePubKey { get; set; }

        public string BitcoinUri => $"bitcoin:{Address}";

        [Required(ErrorMessage = "A label is required")]
        public string ProposedLabel
        {
            get => _proposedLabel;
            set => this.RaiseAndSetIfChanged(ref _proposedLabel, value);
        }

        public bool[,] QrCode
        {
            get => _qrCode;
            set => this.RaiseAndSetIfChanged(ref _qrCode, value);
        }

        public string RequestAmount
        {
            get => _requestAmount;
            set => this.RaiseAndSetIfChanged(ref _requestAmount, value);
        }
    }
}
