using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.PayJoin;
using Chaincase.Common.Services;
using Chaincase.UI.Services;
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
        private readonly P2EPServer _p2epServer;

        public Money ProposedAmount { get; private set; }
        private bool _isBusy;
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
            ProposedLabel = "";
            _notificationManager.RequestAuthorization();
        }

        public async Task TryStartPayjoin(string password)
        {
            IsBusy = true;
            try
            {
                if (P2EPAddress.IsNullOrWhiteSpace())
                {
                    await _p2epServer.TryArmHotWallet(password);

                    var cts = new CancellationToken();
                    _p2epServer.StartAsync(cts);
                    // we don't await because Start listens on the same thread
                    // & so won't return. Is this a code smell?
                }
            }
            catch (SecurityException e)
            {

            }
            finally
            {
                IsBusy = false;
            }
        }

        public string AppliedLabel => ReceivePubKey.Label ?? "";
        public string Address => ReceivePubKey.GetP2wpkhAddress(_config.Network).ToString();
        public string P2EPAddress => _p2epServer.PaymentEndpoint;
        public string Pubkey => ReceivePubKey.PubKey.ToString();
        public string KeyPath => ReceivePubKey.FullKeyPath.ToString();

        public HdPubKey ReceivePubKey { get; set; }

        public string BitcoinUri => $"bitcoin:{Address}";

        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

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
