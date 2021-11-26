using System.Threading.Tasks;
using Chaincase.Common.Contracts;

namespace Chaincase.SSB
{
	public class MockCameraScanner : ICameraScanner
	{
		public Task<string> Scan()
		{
			return Task.FromResult("");
		}
	}
}
