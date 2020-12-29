using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Gma.QrCodeNet.Encoding;
using NBitcoin;
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
		private string _appliedLabel;
		private bool[,] _qrCode;
		private string _requestAmount;
		private ObservableAsPropertyHelper<string> _bitcoinUri;

		public ReceiveViewModel(Global global, IShare share)
		{
			Global = global;
			Global.NotificationManager.RequestAuthorization();

			Share = share;

			//_bitcoinUri = this
			//	.WhenAnyValue(x => x.RequestAmount)
			//	.Select(amount =>
			//	{
			//		return $"bitcoin:{Address}";
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

		private bool IsPasswordValid(string password)
		{
			string walletFilePath = Path.Combine(Global.WalletManager.WalletDirectories.WalletsDir, $"{Global.Network}.json");
			ExtKey keyOnDisk;
			try
			{
				keyOnDisk = KeyManager.FromFile(walletFilePath).GetMasterExtKey(password ?? "");
			}
			catch
			{
				// bad password
				return false;
			}
			return true;
		}

		public bool DidGetNextReceiveKey(string password)
		{
			if (IsPasswordValid(password))
			{
				ReceivePubKey = Global.Wallet.KeyManager.GetNextReceiveKey(ProposedLabel, out bool minGapLimitIncreased);
				ProposedLabel = "";
				return true;
			} else
			{
				return false;
			}
		}

		public async Task ShareBoundString(string boundString)
		{
			await Share.ShareText(boundString);
		}

		public string AppliedLabel => ReceivePubKey.Label ?? "";
		public string Address => ReceivePubKey.GetP2wpkhAddress(Global.Network).ToString();
		public string Pubkey => ReceivePubKey.PubKey.ToString();
		public string KeyPath => ReceivePubKey.FullKeyPath.ToString();

		public HdPubKey ReceivePubKey { get; set; }

		public string BitcoinUri => $"bitcoin:{Address}";

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
