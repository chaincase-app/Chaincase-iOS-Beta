using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WalletWasabi.Backend.Models
{
	public class DeviceToken
	{
		[Key]
		[Required]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public string Token { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public TokenType Type { get; set; }

		[JsonIgnore]
		public TokenStatus Status { get; set; } = TokenStatus.New;
	}

	public enum TokenStatus
	{
		New,
		Valid,
		Invalid
	}

	public enum TokenType
	{
		Apple,
		AppleDebug
	}
}