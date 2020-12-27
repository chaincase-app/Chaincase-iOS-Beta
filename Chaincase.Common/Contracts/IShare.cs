using System.Threading.Tasks;

namespace Chaincase.Common.Contracts
{
	public interface IFileShare
	{
		public Task ShareFile(string file, string title);
	}
}
