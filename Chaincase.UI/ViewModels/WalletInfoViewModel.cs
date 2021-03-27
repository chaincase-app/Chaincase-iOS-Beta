using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using ReactiveUI;

namespace Chaincase.UI.ViewModels
{
	public class WalletInfoViewModel : ReactiveObject
    {
	    private readonly IShare _share;
        private readonly IDataDirProvider _dataDirProvider;
	    protected Global Global { get; }

        public WalletInfoViewModel(Global global, IShare share, IDataDirProvider dataDirProvider)
        {
	        _share = share;
            _dataDirProvider = dataDirProvider;
            Global = global;

	        // var canBackUp = this.WhenAnyValue(x => x.Global.UiConfig.HasSeed, hs => hs == true);

	        //NavBackUpCommand = ReactiveCommand.CreateFromObservable(() =>
	        //{
	        //	ViewStackService.PushPage(new StartBackUpViewModel()).Subscribe();
	        //	return Observable.Return(Unit.Default);
	        //}, canBackUp);

	        //ShareLogsCommand = ReactiveCommand.CreateFromTask(ShareLogs);
	        //ExportWalletCommand = ReactiveCommand.CreateFromTask(ExportWallet);
        }

        public async Task ShareDebugLog()
        {
            var file = Path.Combine(_dataDirProvider.Get(), "Logs.txt");

            await _share.ShareFile(file, "Share Debug Logs");
        }

        public async Task ExportWallet()
        {
            var file = Path.Combine(_dataDirProvider.Get(), $"Wallets/{Global.Network}.json");

            await _share.ShareFile(file, "Export Wallet");
        }

        public string ExtendedAccountPublicKey => Global.Wallet.KeyManager.ExtPubKey.ToString(Global.Network) ?? "";
        public string AccountKeyPath => $"m/{ Global.Wallet.KeyManager.AccountKeyPath}";
    }
}
