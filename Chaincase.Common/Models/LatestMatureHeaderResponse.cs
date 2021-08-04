using System;
using NBitcoin;
using Newtonsoft.Json;
using WalletWasabi.JsonConverters;

namespace Chaincase.Common.Models
{
	public class LatestMatureHeaderResponse
	{
		[JsonConverter(typeof(Uint256JsonConverter))]
		public uint256 BlockHash { get; set; }

		[JsonConverter(typeof(Uint256JsonConverter))]
		public uint256 PrevHash { get; set; }

		public int Height { get; set; }
		public DateTime Time { get; set; }
	}
}
