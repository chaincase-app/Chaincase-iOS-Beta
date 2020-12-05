using System;
using System.Threading.Tasks;
using Hara.Abstractions;
using Hara.Abstractions.Contracts;
using Xamarin.Essentials;

namespace Hara.XamarinCommon.Services
{
    public class XamarinEssentialsWebsiteLauncher : IWebsiteLauncher
    {
        public async Task Launch(Uri uri)
        {
            await Browser.OpenAsync(uri);
        }
    }
}