using System.IO;
using System.Threading.Tasks;

namespace Hara.Abstractions.Contracts
{
    public interface ILocalContentFetcher
    {
        Task<Stream> Fetch(string path);
    }
}