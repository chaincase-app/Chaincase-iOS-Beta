using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Chaincase.Models;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;

namespace Chaincase.ViewModels
{
    public class TransactionViewModel : ViewModelBase
    {
		private bool _clipboardNotificationVisible;
		private double _clipboardNotificationOpacity;

		public TransactionViewModel(TransactionInfo model) :
            base(Locator.Current.GetService<IViewStackService>())
		{
			Model = model;
		}

		private TransactionInfo Model { get; }

		public ReactiveCommand<Unit, Unit> CopyTransactionId { get; }

		public string DateTime => Model.DateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

		public bool Confirmed => Model.Confirmed;

		public int Confirmations => Model.Confirmations;

		public string AmountBtc => Model.AmountBtc;

		public Money Amount => Money.TryParse(Model.AmountBtc, out Money money) ? money : Money.Zero;

		public string Label => Model.Label;

		public int BlockHeight => Model.BlockHeight;

		public string TransactionId => Model.TransactionId;

		public bool ClipboardNotificationVisible
		{
			get => _clipboardNotificationVisible;
			set => this.RaiseAndSetIfChanged(ref _clipboardNotificationVisible, value);
		}

		public double ClipboardNotificationOpacity
		{
			get => _clipboardNotificationOpacity;
			set => this.RaiseAndSetIfChanged(ref _clipboardNotificationOpacity, value);
		}

		public CancellationTokenSource CancelClipboardNotification { get; set; }

		public void Refresh()
		{
			this.RaisePropertyChanged(nameof(AmountBtc));
			this.RaisePropertyChanged(nameof(TransactionId));
			this.RaisePropertyChanged(nameof(DateTime));
		}
	}
}
