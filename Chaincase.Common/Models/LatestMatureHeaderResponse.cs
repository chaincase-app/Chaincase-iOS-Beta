using System;
using NBitcoin;
using NBitcoin.JsonConverters;
using Newtonsoft.Json;
using WalletWasabi.JsonConverters;

namespace Chaincase.Common.Models
{
	public class LatestMatureHeaderResponse
	{
		[JsonConverter(typeof(Uint256JsonConverter))]
		public uint256 MatureHeaderHash { get; set; }
		
		[JsonConverter(typeof(DateTimeOffsetUnixSecondsConverter))]
		public DateTimeOffset MatureHeaderTime { get; set; }
		[JsonConverter(typeof(Uint256JsonConverter))]
		public uint256 MatureHeaderPrevHash { get; set; }
		[JsonConverter(typeof(Uint256JsonConverter))]
		public uint256 BestHeaderHash { get; set; }
		public uint MatureHeight { get; set; }
		public uint BestHeight { get; set; }
	}
}
