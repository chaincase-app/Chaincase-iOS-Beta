using System;
using System.IO;
using System.Threading.Tasks;
using Foundation;

namespace Chaincase.Common.Services
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "iCloud is iCloud")]
    public static class iCloudService
    {
        public static NSUrl iCloudDirectory() =>
            NSFileManager.DefaultManager.GetUrlForUbiquityContainer(null);

        public static NSFileManager FileManager => NSFileManager.DefaultManager;

        //bool HasiCloud;
        //string iCloudUrl;

        //public iCloudService()
        //{
        //    iCloudUrl = GetUrlForUbiquityContainer();
        //}

        //public async Task<string> GetUrlForUbiquityContainer()
        //{
        //    // GetUrlForUbiquityContainer is blocking, Apple recommends background thread or your UI will freeze
        //    Task.Run(() =>
        //    {
        //    var uburl = NSFileManager.DefaultManager.GetUrlForUbiquityContainer(null);

        //    if (uburl == null)
        //    {
        //        HasiCloud = false;
        //        Console.WriteLine("Can't find iCloud container, check your provisioning profile and entitlements");
        //    }
        //    else
        //    { // iCloud enabled, store the NSURL for later use
        //        HasiCloud = true;
        //        iCloudUrl = uburl;
        //        Console.WriteLine("yyy Yes iCloud! {0}", uburl.AbsoluteUrl);
        //    });
        //    // OR instead of null you can specify "TEAMID.com.your-company.ApplicationName"

        //    }
        //}

        public static void MoveToCloud(string tempFile, string destinationPath)
        {
            var ubiquityPath = iCloudDirectory().Path;
            if (ubiquityPath != null)
            {
                var targetFile = Path.Combine(ubiquityPath, destinationPath);
                var dir = Directory.GetParent(targetFile).FullName;
                var name = Path.GetFileName(targetFile);

                var uniqueFile = targetFile;
                int count = 1;
                while (FileManager.FileExists(uniqueFile))
                {
                    var uniqueName = uniqueFile + count;
                    uniqueFile = Path.Combine(dir, uniqueName);
                    count++;
                }

                if (!FileManager.FileExists(uniqueFile))
                {
                    FileManager.CreateDirectory(dir, createIntermediates: true, null);
                }

                NSFileManager.DefaultManager.SetUbiquitous(true, new(tempFile), new(uniqueFile), out NSError error);
                if (error != null)
                {
                    throw new Exception(error.Description);
                }

                File.Delete(tempFile);
            }
            else
            {
                File.Delete(tempFile);
                Console.WriteLine("Can't find iCloud container, check your provisioning profile and entitlements");
            }
        }
    }
}
