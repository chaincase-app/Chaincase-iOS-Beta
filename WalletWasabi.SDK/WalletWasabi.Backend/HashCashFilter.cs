using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NBitcoin.DataEncoders;
using WalletWasabi.Helpers;

namespace WalletWasabi.Backend
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class HashCashFilter : Attribute, IActionFilter
	{
		private readonly string _resource;

		public virtual string GetResource(ActionExecutingContext context)
		{
			return _resource;
		}

		public virtual TimeSpan ChallengeValidFor { get; }
		public virtual int? MinPow { get; }

		protected HashCashFilter()
		{
		}

		public HashCashFilter(string resource, TimeSpan maxDifference, int? minPow)
		{
			_resource = resource;
			ChallengeValidFor = maxDifference;
			MinPow = minPow;
		}

		public void OnActionExecuted(ActionExecutedContext context)
		{
		}

		public void OnActionExecuting(ActionExecutingContext context)
		{

			var config = context.HttpContext.RequestServices.GetRequiredService<Config>();
			var pow = MinPow?? config.HashCashDifficulty;
			var resource = GetResource(context);
			if (resource is null || pow <= 0)
			{
				return;
			}

			var memoryCache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

			if (!context.HttpContext.Request.Headers.TryGetValue("X-Hashcash", out var xhashcashValue))
			{
				context.HttpContext.Response.Headers.Add("X-Hashcash-Error", "hashcash-not-provided");
				context.Result = new BadRequestObjectResult("Missing X-Hashcash header");
				CreateChallenge(resource, pow, memoryCache, context);
				return;
			}

			var fValue = xhashcashValue.First();
			var challenge = fValue.Substring(0, fValue.LastIndexOf(":", StringComparison.InvariantCultureIgnoreCase));
			var cacheKey = $"{nameof(HashCashFilter)}_challenge_{challenge}";
			if(!memoryCache.TryGetValue(cacheKey, out _))
			{
				context.HttpContext.Response.Headers.Add("X-Hashcash-Error", "challenge-invalid");
				context.Result = new BadRequestObjectResult(
					$"Invalid hashcash: challenge not found");
				CreateChallenge(resource, pow, memoryCache, context);
				return;
			}

			memoryCache.Remove(cacheKey);
			if (HashCashUtils.Verify(fValue))
			{
				return;
			}
			CreateChallenge(resource, pow, memoryCache, context);

			context.HttpContext.Response.Headers.Add("X-Hashcash-Error", "hashcash-invalid");
			context.Result = new BadRequestObjectResult(
				$"Invalid hashcash");
		}

		private void CreateChallenge(string resource, int pow, IMemoryCache memoryCache, ActionExecutingContext context)
		{
			var expiry = DateTimeOffset.UtcNow.Add(ChallengeValidFor);
			var challenge = HashCashUtils.GenerateChallenge(resource, expiry, pow);

			var cacheKey = $"{nameof(HashCashFilter)}_challenge_{challenge}";
			memoryCache.CreateEntry(cacheKey).AbsoluteExpiration = expiry;
			context.HttpContext.Response.Headers.Add("X-Hashcash-Challenge", new StringValues(challenge));
		}
	}
}