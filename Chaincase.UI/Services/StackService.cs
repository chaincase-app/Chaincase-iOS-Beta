using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chaincase.UI.Services
{
	public class StackService
	{
		public event Action StackStateChanged;
		private Stack<Func<Task>> _stackedStates = new Stack<Func<Task>>();
		public bool AnyStackState => _stackedStates.Any();

		public Task InvokeStackState()
		{
			if (AnyStackState)
			{
				return _stackedStates.Pop().Invoke().ContinueWith(task => StackStateChanged?.Invoke());
			}

			return Task.CompletedTask;
		}

		public Task PushStackState(Func<Task> act)
		{
			_stackedStates.Push(act);
			StackStateChanged?.Invoke();
			return Task.CompletedTask;
		}

		public Task ClearStack()
		{
			if (AnyStackState)
			{
				_stackedStates.Clear();
				StackStateChanged?.Invoke();
			}
            
			return Task.CompletedTask;
		}
	}
}
