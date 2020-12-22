using System.IO;
using Chaincase.Common.Contracts;
using WalletWasabi.Helpers;

namespace Chaincase.SSB
{
	public class SSBDataDirProvider:IDataDirProvider
	{
		public string Get()
		{
			return EnvironmentHelpers.GetDataDir(Path.Combine("Chaincase", "Client"));
		}
	}
}
