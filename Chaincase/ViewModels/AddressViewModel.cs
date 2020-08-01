using Chaincase.Navigation;
using Gma.QrCodeNet.Encoding;
using ReactiveUI;
using Splat;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.Keys;
using Xamarin.Essentials;

namespace Chaincase.ViewModels
{
	public class AddressViewModel : ViewModelBase
	{
		protected Global Global { get; }

		private bool[,] _qrCode;
		private RequestAmountViewModel _requestAmountViewModel;
		private ObservableAsPropertyHelper<string> _bitcoinUri;

		public AddressViewModel(HdPubKey model)
			: base(Locator.Current.GetService<IViewStackService>())
		{
			Global = Locator.Current.GetService<Global>();

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

			_bitcoinUri = this
				.WhenAnyValue(x => x.RequestAmountViewModel.RequestAmount)
				.Select(amount => {
					var uri = $"bitcoin:{Address}";

					if (amount is { })
						uri += $"?amount={amount}";

					return uri;
				})
				.ToProperty(this, nameof(BitcoinUri));

			RequestAmountCommand = ReactiveCommand.CreateFromObservable<Unit, Unit>(_ =>
			{
				if (RequestAmountViewModel is null)
					RequestAmountViewModel = new RequestAmountViewModel();

				ViewStackService.PushModal(RequestAmountViewModel).Subscribe();
				return Observable.Return(Unit.Default);
			});
			ShareCommand = ReactiveCommand.CreateFromTask<string>(ShareAddress);
			NavWalletCommand = ReactiveCommand.CreateFromObservable<Unit, Unit>(_ =>
			{
				ViewStackService.PopPage(false);
				return ViewStackService.PopPage(true);
		    });
		}

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

		public string BitcoinUri => _bitcoinUri.Value;

		public ReactiveCommand<Unit, Unit> RequestAmountCommand;
		public ReactiveCommand<string, Unit> ShareCommand;
		public ReactiveCommand<Unit, Unit> NavWalletCommand;

		public async Task ShareAddress(string address)
        {
			await Share.RequestAsync(new ShareTextRequest
			{
				Text = address
			});
        }

		public RequestAmountViewModel RequestAmountViewModel
		{
			get => _requestAmountViewModel;
			set => this.RaiseAndSetIfChanged(ref _requestAmountViewModel, value);
		}
	}
}
