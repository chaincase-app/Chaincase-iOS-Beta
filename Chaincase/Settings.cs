using System;
using System.IO;
using Plugin.Settings;
using Plugin.Settings.Abstractions;
using WalletWasabi.Helpers;

namespace Chaincase
{
	public static class Settings
	{
		private static ISettings AppSettings
		{
			get
			{
				return CrossSettings.Current;
			}
		}
		public static readonly string DataDirDefault = EnvironmentHelpers.GetDataDir(Path.Combine("Wasabi", "Client"));
		public static readonly string WalletsDirDefault = Path.Combine(DataDir, "Wallets");

		public static string DataDir
		{
			get
			{
				return AppSettings.GetValueOrDefault(nameof(DataDir), DataDirDefault);
			}
			set
			{
				AppSettings.AddOrUpdateValue(nameof(DataDir), value);
			}
		}

		public static string WalletsDir
		{
			get
			{
				return AppSettings.GetValueOrDefault(nameof(WalletsDir), WalletsDirDefault);
			}
			set
			{
				AppSettings.AddOrUpdateValue(nameof(WalletsDir), value);
			}
		}
	}
}