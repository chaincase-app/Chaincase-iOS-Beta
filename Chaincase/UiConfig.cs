using Newtonsoft.Json;
using System.ComponentModel;
using WalletWasabi.Bases;


namespace Chaincase
{
	[JsonObject(MemberSerialization.OptIn)]
	public class UiConfig : ConfigBase
	{
        private string _balance;

		[DefaultValue("0")]
		[JsonProperty(PropertyName = "Balance", DefaultValueHandling = DefaultValueHandling.Populate)]
		public string Balance
		{
			get => _balance;
			set => RaiseAndSetIfChanged(ref _balance, value);
		}

		public UiConfig() : base()
		{
		}

		public UiConfig(string filePath) : base(filePath)
		{
		}
	}
}
