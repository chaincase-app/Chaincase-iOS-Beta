using Chaincase.Navigation;
using Gma.QrCodeNet.Encoding;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.Keys;
using Xamarin.Essentials;

namespace Chaincase.ViewModels
{
	public class AddressViewModel : BaseViewModel
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

		public AddressViewModel(IViewStackService viewStackService, HdPubKey model) : base(viewStackService)
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
		}

		public ReactiveCommand<string, Unit> ShareCommand;

        public async Task ShareAddress(string address)
        {
			await Share.RequestAsync(new ShareTextRequest
			{
				Text = address
			});
        }
	}
}
