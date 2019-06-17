using System;
using System.IO;
using WalletWasabi.Helpers;
namespace Wasabi
{
	public static class Global
	{
		public static string DataDir { get; }
		public static string WalletsDir { get; }


		static Global()
		{
			DataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Wasabi", "Client"));
			WalletsDir = Path.Combine(DataDir, "Wallets");
		}
	}
}
