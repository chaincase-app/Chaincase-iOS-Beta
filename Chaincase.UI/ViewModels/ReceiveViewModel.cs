using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Gma.QrCodeNet.Encoding;
using ReactiveUI;
using Splat;
using WalletWasabi.Blockchain.Keys;

namespace Chaincase.UI.ViewModels
{
	public class ReceiveViewModel : ReactiveObject
	{
		protected Global Global { get; }
		protected IShare Share { get; }

		private string _proposedLabel;
		private bool[,] _qrCode;
		private string _requestAmount;
		private ObservableAsPropertyHelper<string> _bitcoinUri;

		public ReceiveViewModel(Global global, IShare share)
		{
			Global = global;
			///Global.NotificationManager.RequestAuthorization();

			Share = share;

			//_bitcoinUri = this
			//	.WhenAnyValue(x => x.RequestAmount)
			//	.Select(amount => {
			//		return $"bitcoin:{Address}?amount={amount}";
			//	})
			//	.ToProperty(this, nameof(BitcoinUri));

			//this.WhenAnyValue(x => x.BitcoinUri)
			//	.Subscribe((uri) => EncodeQRCode());

		}

		private void EncodeQRCode()
		{
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

		private async Task<bool> PromptPassword(string validPassword)
		{
			//if (validPassword != null)
			//{
			//	return false;
			//Device.BeginInvokeOnMainThread(() =>
			//			{
			//				HdPubKey toReceive = Global.Wallet.KeyManager.GetNextReceiveKey(Memo, out bool minGapLimitIncreased);
			//Memo = "";
			//				// move to address page
			//}
			return true;
		}

		public async Task ShareBoundString(string boundString)
		{
			await Share.ShareText(boundString);
		}
		public string AppliedLabel => Model.Label;
		public string Address => Model.GetP2wpkhAddress(Global.Network).ToString();
		public string Pubkey => Model.PubKey.ToString();
		public string KeyPath => Model.FullKeyPath.ToString();

		public HdPubKey Model { get; }

		public string BitcoinUri => _bitcoinUri.Value is { } ? _bitcoinUri.Value : $"bitcoin:{Address}";

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
