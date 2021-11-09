using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.BlockFilters;
using WalletWasabi.Helpers;

namespace WalletWasabi.Backend.Controllers
{
	[Produces("application/json")]
	[Route("api/v" + Constants.BackendMajorVersion + "/btc/[controller]")]
	public class MempoolController : Controller
	{
		private readonly MempoolIndexBuilderService _mempoolIndexBuilderService;

		public MempoolController(MempoolIndexBuilderService mempoolIndexBuilderService)
		{
			_mempoolIndexBuilderService = mempoolIndexBuilderService;
		}

		[HttpGet("root")]
		[ProducesResponseType(200)]
		public async Task<IActionResult> GetRootFilter()
		{
			if (_mempoolIndexBuilderService.LastBuilt is null)
			{
				return BadRequest("the mempool filters have not yet been built");
			}

			return Json(new Dictionary<string, string>()
				{ {_mempoolIndexBuilderService.RootFilterKey, _mempoolIndexBuilderService.RootFilter.ToString()} });
		}

		[HttpGet("sub")]
		[ProducesResponseType(200)]
		public async Task<IActionResult> GetSubFilters()
		{
			if (_mempoolIndexBuilderService.LastBuilt is null)
			{
				return BadRequest("the mempool filters have not yet been built");
			}
			return Json(_mempoolIndexBuilderService.SubFilters.ToDictionary(pair => pair.Key, pair => pair.Value.ToString()));
		}

		[HttpGet("buckets")]
		[ProducesResponseType(200)]
		public async Task<IActionResult> GetFilterBuckets(string[] keys)
		{
			if (_mempoolIndexBuilderService.LastBuilt is null)
			{
				return BadRequest("the mempool filters have not yet been built");
			}

			return Json(_mempoolIndexBuilderService.Buckets.Where(pair => keys.Contains(pair.Key))
				.SelectMany(pair => pair.Value.Select(transaction => transaction.ToHex())));
		}
	}
}
