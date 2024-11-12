using Microsoft.AspNetCore.Mvc;
using backend.Services;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketDataController : ControllerBase
    {

        private readonly MarketDataService _marketDataService;

        public MarketDataController(MarketDataService marketDataService)
        {
            _marketDataService = marketDataService;
        }

        [HttpGet("unaccessed-day")]
        public async Task<IActionResult> GetUnaccessedDay(int userId)
        {
            var unaccessedDay = await _marketDataService.GetUnaccessedDay(userId);

            if (unaccessedDay == null)
            {
                return NotFound("No unaccessed day available for this user.");
            }

            return Ok(unaccessedDay);
        }



        [HttpPost("fetch-data")]
        public async Task<IActionResult> FetchAndSaveData()
        {
            await _marketDataService.FetchAndSaveMonthlyData();
            return Ok("Data fetched and saved successfully.");
        }
    }

}
