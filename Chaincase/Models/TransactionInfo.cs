using Newtonsoft.Json;
using ReactiveUI;
using System;

namespace Chaincase.Models
{
	public class TransactionInfo : ReactiveObject
	{
		[JsonProperty]
		private int _confirmations;
		[JsonProperty]
		private bool _confirmed;
		[JsonProperty]
		private DateTimeOffset _dateTime;
		[JsonProperty]
		private string _amountBtc;
		[JsonProperty]
		private string _label;
		[JsonProperty]
		private int _blockHeight;
		[JsonProperty]
		private string _transactionId;

		public DateTimeOffset DateTime
		{
			get => _dateTime;
			set => this.RaiseAndSetIfChanged(ref _dateTime, value);
		}

		public int Confirmations
		{
			get => _confirmations;
			set => this.RaiseAndSetIfChanged(ref _confirmations, value);
		}

		public bool Confirmed
		{
			get => _confirmed;
			set => this.RaiseAndSetIfChanged(ref _confirmed, value);
		}

		public string AmountBtc
		{
			get => _amountBtc;
			set => this.RaiseAndSetIfChanged(ref _amountBtc, value);
		}

		public string Label
		{
			get => _label;
			set => this.RaiseAndSetIfChanged(ref _label, value);
		}

		public int BlockHeight
		{
			get => _blockHeight;
			set => this.RaiseAndSetIfChanged(ref _blockHeight, value);
		}

		public string TransactionId
		{
			get => _transactionId;
			set => this.RaiseAndSetIfChanged(ref _transactionId, value);
		}
	}
}
