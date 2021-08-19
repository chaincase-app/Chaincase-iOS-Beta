using System;
using System.IO;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services;
using Foundation;
using WalletWasabi.Logging;
using WalletWasabi.Wallets;

namespace Chaincase.iOS.Services
{
    public class iOSWalletDirectories : WalletDirectories
    {
        public iOSWalletDirectories(IDataDirProvider dataDirProvider) :
            base(dataDirProvider.Get())
        {
            CreateBackgroundAccessibleDirectory(WalletsDir);
            CreateBackgroundAccessibleDirectory(WalletsBackupDir);

            // TODO if there's a backwards incompatible change this can be elminated.
            // a new file inherits permissions of the folder it's created in
            foreach (var file in EnumerateWalletFiles())
            {
                try
                {
                    new NSUrl(file.FullName)
                        .SetResource(NSUrl.FileProtectionKey, NSUrl.FileProtectionCompleteUntilFirstUserAuthentication);
                }
                catch
                {
                    Logger.LogWarning($"Could not set CompleteUntilFirstUserAuthentication Protection at {file.FullName}");
                }
            }
        }

        private static bool CreateBackgroundAccessibleDirectory(string dir)
        {
            try
            {
                return NSFileManager.DefaultManager.CreateDirectory(
                    dir,
                    createIntermediates: true,
                    new NSFileAttributes()
                    {
                        ProtectionKey = NSFileProtection.CompleteUntilFirstUserAuthentication
                    });
            }
            catch
            {
                Logger.LogWarning($"Could not set CompleteUntilFirstUserAuthentication Protection at {dir}");
            }
            return false;
        }
    }
}
