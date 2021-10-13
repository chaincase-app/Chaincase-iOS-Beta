using System;
using Chaincase.Common;
using ReactiveUI;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using Chaincase.Common.Services;
using System.Net;
using WalletWasabi.Exceptions;
using System.Globalization;
using System.Threading;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;

namespace Chaincase.UI.ViewModels
{
    public class ConnectNodeViewModel : ReactiveObject
    {
        private readonly Global _global;
        private readonly ChaincaseWalletManager _walletManager;
        private readonly Config _config;
        private readonly UiConfig _uiConfig;
        private readonly FeeProviders _feeProviders;
        private readonly ChaincaseSynchronizer _synchronizer;

        private string _nodeAddress;
        private bool _hasSetCustomAddress;

        public ConnectNodeViewModel(Global global, ChaincaseWalletManager walletManager, Config config, ChaincaseSynchronizer synchronizer)
        {
            _global = global;
            _walletManager = walletManager;
            _config = config;
            _synchronizer = synchronizer;
            // TODO If user has not set up full node before, load some default values
            var currentMainNetUri = new Uri($"{_config.DefaultMainNetP2PHost}:{_config.DefaultMainNetP2PPort}");
            var currentTestNetUri = new Uri($"{_config.DefaultTestNetP2PHost}:{_config.DefaultTestNetP2PPort}");
            if (currentMainNetUri == _config.GetCurrentBackendUri() || currentTestNetUri == _config.GetCurrentBackendUri()) {
                // TODO 
                //_nodeHost = "http://localhost";
            }
        }

        public void SetDefaultNodeAddress() {
            if (_config.Network == NBitcoin.Network.Main)
            {
                _config.SetP2PEndpoint(CreateEndPoint($"{_config.DefaultMainNetP2PHost}:{_config.DefaultMainNetP2PPort}"));
            }
            else if (_config.Network == NBitcoin.Network.TestNet)
            {
                _config.SetP2PEndpoint(CreateEndPoint($"{_config.DefaultTestNetP2PHost}:{_config.DefaultTestNetP2PPort}"));
            }
            else {
                throw new NotSupportedNetworkException(_config.Network);
            }
        }

        public void SetNodeAddress() {
            // Timeout after 10 seconds
            var cancellationToken = new CancellationToken();
            using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            handshakeTimeout.CancelAfter(TimeSpan.FromSeconds(10));
            var nodeConnectionParameters = new NodeConnectionParameters()
            {
                ConnectCancellation = handshakeTimeout.Token,
                IsRelay = false,
            };
            nodeConnectionParameters.TemplateBehaviors.Add(new SocksSettingsBehavior(_synchronizer.WasabiClient.TorClient.TorSocks5EndPoint, onlyForOnionHosts: true, networkCredential: null, streamIsolation: false));

            Node.Connect(_config.Network, _nodeAddress, nodeConnectionParameters);
            var endpoint = CreateEndPoint(_nodeAddress);
            if (_config.Network == NBitcoin.Network.Main)
            {
                _config.SetP2PEndpoint(endpoint);
            }
            else if (_config.Network == NBitcoin.Network.TestNet)
            {
                _config.SetP2PEndpoint(endpoint);
            }
            else
            {
                throw new NotSupportedNetworkException(_config.Network);
            }
        }

        
        private EndPoint CreateEndPoint(string address)
        {
            string[] ep = address.Split(':');
            if (ep.Length != 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
            int port;
            if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }

        public string NodeAddress {
            get => _nodeAddress;
            set => this.RaiseAndSetIfChanged(ref _nodeAddress, value.Trim());
        }

        public string CurrentP2PAddress {
            get => _config.Network == NBitcoin.Network.Main ? _config.MainNetBitcoinP2pEndPoint.ToString() : _config.TestNetBitcoinP2pEndPoint.ToString();
        }

    }
}
