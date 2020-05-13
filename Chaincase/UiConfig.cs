using Chaincase.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using WalletWasabi.Bases;
using WalletWasabi.Blockchain.Transactions;

namespace Chaincase
{
	[JsonObject(MemberSerialization.OptIn)]
	public class UiConfig : ConfigBase
	{
        private string _balance;
		private TransactionInfo[] _transactions;

		[DefaultValue("0")]
		[JsonProperty(PropertyName = "Balance", DefaultValueHandling = DefaultValueHandling.Populate)]
		public string Balance
		{
			get => _balance;
			set => RaiseAndSetIfChanged(ref _balance, value);
		}

		[JsonProperty(PropertyName = "Transactions")]
		public TransactionInfo[] Transactions
		{
			get => _transactions;
			set => RaiseAndSetIfChanged(ref _transactions, value);
		}
    
		public UiConfig() : base()
		{
		}

		public UiConfig(string filePath) : base(filePath)
		{
		}
	}
}
