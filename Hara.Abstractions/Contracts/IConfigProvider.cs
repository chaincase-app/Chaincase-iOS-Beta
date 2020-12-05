using System.Threading.Tasks;

namespace Hara.Abstractions.Contracts
{
    public interface IConfigProvider
    {
        Task<T> Get<T>(string key);
        Task Set<T>(string key, T value);
    }
}