using System;
using System.Threading.Tasks;

namespace Hara.Abstractions.Contracts
{
    public interface IWebsiteLauncher
    {
        Task Launch(Uri uri);
    }
}