using Gma.QrCodeNet.Encoding;
using ReactiveUI;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase.ViewModels
{
	public class AddressViewModel : ViewModelBase
	{
		private bool[,] _qrCode;

		public string Label => Model.Label;
		public string Address => Model.GetP2wpkhAddress(Global.Network).ToString();
		public string Pubkey => Model.PubKey.ToString();
		public string KeyPath => Model.FullKeyPath.ToString();
		public bool[,] QrCode
		{
			get => _qrCode;
			set => this.RaiseAndSetIfChanged(ref _qrCode, value);
		}
		public HdPubKey Model { get; }

		public AddressViewModel(IScreen hostScreen, HdPubKey model) : base(hostScreen)
		{
			Model = model;

			// TODO fix this performance issue this should only be generated when accessed.
			Task.Run(() =>
			{
				var encoder = new QrEncoder(ErrorCorrectionLevel.M);
				encoder.TryEncode(Address, out var qrCode);

				return qrCode.Matrix.InternalArray;
			}).ContinueWith(x =>
			{
				QrCode = x.Result;
			});
		}
	}
}
