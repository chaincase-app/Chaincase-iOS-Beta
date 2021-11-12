using System;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NBitcoin;
using NBitcoin.RPC;
using WalletWasabi.BitcoinCore;
using WalletWasabi.Tests.XunitConfiguration;
using Xunit.Sdk;

namespace Chaincase.Tests
{
	public static class TestUtils
	{
		public static bool InCi => Environment.GetEnvironmentVariable("IN_CI") == "true";

		public static RegTestFixture CreateRegtestFixture(bool useDeployedNode = true, bool newDb = false )
		{
			CoreNode coreNode = null;
			if (useDeployedNode)
			{
				//We cannot use Moq here due to the props not being set virtual and we cannot just create a pure object and set normally because they have internal setters
				coreNode = new CoreNode();
				SetPrivateValue(coreNode, node => node.Network, Network.RegTest);
				
				if (InCi)
				{
					SetPrivateValue(coreNode, node => node.RpcEndPoint, new DnsEndPoint("bitcoind", 18443));
					SetPrivateValue(coreNode, node => node.P2pEndPoint, new DnsEndPoint("bitcoind", 18444));
				}
				else
				{
					SetPrivateValue(coreNode, node => node.RpcEndPoint, new IPEndPoint(IPAddress.Loopback, 18443));
					SetPrivateValue(coreNode, node => node.P2pEndPoint, new IPEndPoint(IPAddress.Loopback, 18444));
				}
				SetPrivateValue(coreNode, node => node.RpcEndPoint, new IPEndPoint(IPAddress.Loopback, 18443));
				SetPrivateValue(coreNode, node => node.P2pEndPoint, new IPEndPoint(IPAddress.Loopback, 18444));
				SetPrivateValue(coreNode, node => node.RpcClient, new RpcClientBase(
					new RPCClient(RPCCredentialString.Parse($"ceiwHEbqWI83:DwubwWsoo3"),
						new Uri("http://localhost:18443"), Network.RegTest)));
			}
			var dbName = newDb ? GenerateString(10) : "wasabibackend";
			
			return InCi ? new RegTestFixture(coreNode, $"User ID=postgres;Host=postgres;Port=5432;Database={dbName};", null) : 
				new RegTestFixture(coreNode, newDb? $"User ID=postgres;Host=127.0.0.1;Port=65466;Database={dbName};" : null);
		}

		public static string GenerateString(int length)
		{
			var r = new Random(RandomUtils.GetInt32());

			var letters = new char[length];

			for (var i = 0; i < length; i++)
			{
				letters[i] = (char) (r.Next('A', 'Z' + 1));
			}
			return new string(letters);
		}
		
		private static string GetMemberName(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.MemberAccess:
					return ((MemberExpression)expression).Member.Name;
				case ExpressionType.Convert:
					return GetMemberName(((UnaryExpression)expression).Operand);
				case ExpressionType.Lambda:
					return GetMemberName(((LambdaExpression)expression).Body);
				default:
					throw new NotSupportedException(expression.NodeType.ToString());
			}
		}

		public static void SetPrivateValue<T, TY>(T obj, Expression<Func<T, TY>> expression, TY value)
		{
			var I = obj.GetType().GetProperty(GetMemberName(expression), BindingFlags.Public | BindingFlags.Instance);
			I!.SetValue(obj, value);
		}

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
