using System;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;

namespace Chaincase.SSB
{
	public class SSBMainThreadInvoker:IMainThreadInvoker
	{
		public void Invoke(Action action)
		{
			Task.Run(action);
		}
	}
}
