using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit.Sdk;

namespace Chaincase.Tests
{
	public static class TestUtils
	{
		public static void Eventually(Action act, int ms = 20_000)
		{
			CancellationTokenSource cts = new CancellationTokenSource(ms);
			while (true)
			{
				try
				{
					act();
					break;
				}
				catch (XunitException) when (!cts.Token.IsCancellationRequested)
				{
					cts.Token.WaitHandle.WaitOne(500);
				}
				catch (PlaywrightException) when (!cts.Token.IsCancellationRequested)
				{
					cts.Token.WaitHandle.WaitOne(500);
				}
			}
		}

		public static async Task EventuallyAsync(Func<Task> act, int delay = 20000)
		{
			CancellationTokenSource cts = new CancellationTokenSource(delay);
			while (true)
			{
				try
				{
					await act();
					break;
				}
				catch (XunitException) when (!cts.Token.IsCancellationRequested)
				{
					await Task.Delay(500, cts.Token);
				}
				catch (PlaywrightException) when (!cts.Token.IsCancellationRequested)
				{
					cts.Token.WaitHandle.WaitOne(500);
				}
			}
		}
	}
}
