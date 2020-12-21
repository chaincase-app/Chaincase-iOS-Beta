using System.IO;
using System.Threading.Tasks;
using Chaincase.Common;
using ReactiveUI;
using Splat;
using Xamarin.Essentials;

namespace Chaincase.UI.ViewModels
{
	public class WalletInfoViewModel : ReactiveObject
    {
        protected Global Global { get; }

        public WalletInfoViewModel(Global global)
        {
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
            var file = Path.Combine(Global.DataDir, "Logs.txt");

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Share Debug Logs",
                File = new ShareFile(file)
            });
        }

        public async Task ExportWallet()
        {
            var file = Path.Combine(Global.DataDir, $"Wallets/{Global.Network}.json");

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Export Wallet",
                File = new ShareFile(file)
            });
        }

        public string ExtendedAccountPublicKey => Global.Wallet.KeyManager.ExtPubKey.ToString(Global.Network) ?? "";
        public string AccountKeyPath => $"m/{ Global.Wallet.KeyManager.AccountKeyPath}";
    }
}
