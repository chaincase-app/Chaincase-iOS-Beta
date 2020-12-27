using System.Threading.Tasks;

namespace Chaincase.Common.Contracts
{
	public interface IShare
	{
		public Task ShareText(string text, string title = "Share");
		public Task ShareFile(string file, string title = "Share");
	}
}
