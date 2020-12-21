using System;
using Chaincase.Common.Contracts;
using Xamarin.Forms;

namespace Chaincase.Services
{
	public class XamarinMainThreadInvoker : IMainThreadInvoker
	{
		public void Invoke(Action action)
		{
			Device.BeginInvokeOnMainThread(action);
		}
	}
}
