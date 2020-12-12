using System;
using System.Threading.Tasks;

namespace Chaincase.Common.Contracts
{
    public interface IWebsiteLauncher
    {
        Task Launch(Uri uri);
    }
}