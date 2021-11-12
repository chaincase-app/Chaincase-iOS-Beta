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
	}
}
