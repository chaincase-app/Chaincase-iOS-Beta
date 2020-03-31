using Chaincase.Navigation;
using Gma.QrCodeNet.Encoding;
using ReactiveUI;
using Splat;
using System.Reactive;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.Keys;
using Xamarin.Essentials;

namespace Chaincase.ViewModels
{
	public class AddressViewModel : ViewModelBase
	{
		private bool[,] _qrCode;

		public string Memo => Model.Label;
		public string Address => Model.GetP2wpkhAddress(Global.Network).ToString();
		public string Pubkey => Model.PubKey.ToString();
		public string KeyPath => Model.FullKeyPath.ToString();
		public bool[,] QrCode
		{
			get => _qrCode;
			set => this.RaiseAndSetIfChanged(ref _qrCode, value);
		}
		public HdPubKey Model { get; }

		public AddressViewModel(HdPubKey model)
			: base(Locator.Current.GetService<IViewStackService>())
		{
			Model = model;

			Task.Run(() =>
			{
				var encoder = new QrEncoder(ErrorCorrectionLevel.M);
				encoder.TryEncode(Address, out var qrCode);

				return qrCode.Matrix.InternalArray;
			}).ContinueWith(x =>
			{
				QrCode = x.Result;
			});

			ShareCommand = ReactiveCommand.CreateFromTask<string>(ShareAddress);
			NavWalletCommand = ReactiveCommand.CreateFromObservable<Unit, Unit>(_ =>
			{
				return ViewStackService.PushPage(new MainViewModel(), null, true);
		    });
		}

		public ReactiveCommand<string, Unit> ShareCommand;
		public ReactiveCommand<Unit, Unit> NavWalletCommand;

		public async Task ShareAddress(string address)
        {
			await Share.RequestAsync(new ShareTextRequest
			{
				Text = address
			});
        }
	}
}
