using System.Threading.Tasks;

namespace Chaincase.Common.Contracts
{
	public interface IClipboard
	{
		Task Copy(string text);

		Task<string> Paste();
	}
}
