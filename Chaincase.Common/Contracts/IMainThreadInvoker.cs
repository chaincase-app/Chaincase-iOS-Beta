using System;

namespace Chaincase.Common.Contracts
{
	public interface IMainThreadInvoker
	{
		void Invoke(Action action);
	}
}
