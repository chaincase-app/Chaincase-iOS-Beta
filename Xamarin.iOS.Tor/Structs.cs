using System.Runtime.InteropServices;

static class CFunctions
{
	// extern void TORInstallEventLogging ();
	[DllImport ("__Internal")]
	static extern void TORInstallEventLogging ();

	// extern void TORInstallEventLoggingCallback (tor_log_cb _Nonnull cb);
	//[DllImport ("__Internal")]
	//[Verify (PlatformInvoke)]
	//static extern unsafe void TORInstallEventLoggingCallback (tor_log_cb* cb);

	// extern void TORInstallTorLogging ();
	[DllImport ("__Internal")]
	static extern void TORInstallTorLogging ();

	// extern void TORInstallTorLoggingCallback (tor_log_cb _Nonnull cb);
	//[DllImport ("__Internal")]
	//[Verify (PlatformInvoke)]
	//static extern unsafe void TORInstallTorLoggingCallback (tor_log_cb* cb);
}
