using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NicolasDorier.RateLimits;
using WalletWasabi.Backend.Data;
using WalletWasabi.Backend.Models;
using WalletWasabi.Backend.Polyfills;
using WalletWasabi.Helpers;

namespace WalletWasabi.Backend.Controllers
{
	[Route("api/v" + Constants.BackendMajorVersion + "/[controller]")]
	[ApiController]
	[Produces("application/json")]
	public class NotificationTokensController : ControllerBase
	{
		private class DeviceTokenHashCashFilter : HashCashFilter
		{
			public override TimeSpan ChallengeValidFor { get; } = TimeSpan.FromMinutes(5);
			public override string GetResource(ActionExecutingContext context)
			{
				switch (context.HttpContext.Request.Method)
				{
					case "PUT":
						var dt = context.ActionArguments.Values.SingleOrDefault(pair => pair is DeviceToken) as DeviceToken;
						return dt?.Token;
					case "DELETE":
						return context.RouteData.Values.TryGetValue("tokenString", out var tokenString) ? $"{tokenString}_delete" : null;
				}
				return base.GetResource(context);
			}
		}

		private readonly IDbContextFactory<WasabiBackendContext> ContextFactory;

		public NotificationTokensController(IDbContextFactory<WasabiBackendContext> contextFactory)
		{
			ContextFactory = contextFactory;
		}

		[HttpPut]
		[ProducesResponseType(200)]
		[ProducesResponseType(400)]
		[RateLimitsFilter(ZoneLimits.NotificationTokens, Scope = RateLimitsScope.RemoteAddress)]
		[DeviceTokenHashCashFilter]
		public async Task<IActionResult> StoreTokenAsync([FromBody] DeviceToken token)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest("Invalid device token.");
			}
			await using var context = ContextFactory.CreateDbContext();
			var existingToken = await context.Tokens.FindAsync(token.Token);
			if (existingToken != null)
			{
				existingToken.Status = token.Status;
				existingToken.Type = token.Type;
			}
			else
			{
				await context.Tokens.AddAsync(token);
			}
			await context.SaveChangesAsync();
			return Ok("Device token stored.");
		}

		/// <summary>
		/// Removes a device token so that device stops receiving notifications.
		/// </summary>
		/// <param name="tokenString">An Apple device token</param>
		/// <response code="200">Always return Ok, we should not confirm whether a token is in the db or not here</response>
		[HttpDelete("{tokenString}")]
		[ProducesResponseType(200)]
		[DeviceTokenHashCashFilter]
		[RateLimitsFilter(ZoneLimits.NotificationTokens, Scope = RateLimitsScope.RouteData, DataKey = "tokenString")]
		public async Task<IActionResult> DeleteTokenAsync([FromRoute] string tokenString)
		{
			await using var context = ContextFactory.CreateDbContext();
			var token = await context.Tokens.FindAsync(tokenString);
			if (token == null)
			{
				return Ok();
			}

			context.Tokens.Remove(token);
			await context.SaveChangesAsync();
			return Ok();
		}
	}
}
