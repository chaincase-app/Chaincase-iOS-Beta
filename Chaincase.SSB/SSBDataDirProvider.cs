using System.IO;
using Chaincase.Common.Contracts;
using WalletWasabi.Helpers;

namespace Chaincase.SSB
{
	public class SSBDataDirProvider:IDataDirProvider
	{
		protected string SubDirectory;

		public SSBDataDirProvider()
		{
			SubDirectory = Path.Combine("Chaincase", "Client");
			Directory.CreateDirectory(Get());
		}
		public string Get()
		{
			return EnvironmentHelpers.GetDataDir(SubDirectory);
		}
	}
}
