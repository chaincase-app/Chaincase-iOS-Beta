using System;
using System.IO;
using Chaincase.Common.Contracts;
using WalletWasabi.Helpers;
using Xamarin.Forms;

namespace Chaincase.Services
{
	public class XamarinDataDirProvider : IDataDirProvider
	{
		public string Get()
		{
			string dataDir;
			if (Device.RuntimePlatform == Device.iOS)
			{
				var library = Environment.GetFolderPath(Environment.SpecialFolder.Resources);
				var client = Path.Combine(library, "Client");
				dataDir = client;
			}
			else
			{
				dataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Chaincase", "Client"));
			}

			return dataDir;
		}
	}
}
