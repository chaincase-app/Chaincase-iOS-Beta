using System.Threading.Tasks;

namespace Chaincase.Common.Contracts
{
	public interface ICameraScanner
	{
		Task<string> Scan();
	}
}
