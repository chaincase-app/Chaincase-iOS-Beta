using System;
using Chaincase.Common;
using ReactiveUI;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using Chaincase.Common.Services;

namespace Chaincase.UI.ViewModels
{
    public class ConnectNodeViewModel : ReactiveObject
    {
        private readonly Global _global;
        private readonly ChaincaseWalletManager _walletManager;
        private readonly Config _config;
        private readonly UiConfig _uiConfig;
        private readonly FeeProviders _feeProviders;

        private string _nodeAddress;

        public ConnectNodeViewModel(Global global, ChaincaseWalletManager walletManager, Config config)
        {
            _global = global;
            _walletManager = walletManager;
            _config = config;
        }

        public void SetNodeAddress() {
            // Verify node address
        }

        public string NodeAddress {
            get => _nodeAddress;
            set => this.RaiseAndSetIfChanged(ref _nodeAddress, value);
        }
    }
}
