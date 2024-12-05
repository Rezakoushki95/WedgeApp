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

        /// <summary>
        /// Fetch and save a random historical month. Intended for testing or development use.
        /// </summary>
        [HttpPost("fetch-random-month")]
        public async Task<IActionResult> FetchRandomHistoricalMonth()
        {
            try
            {
                await _marketDataService.FetchRandomHistoricalMonth();
                return Ok("Random historical month fetched and saved successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching random historical data.", details = ex.Message });
            }
        }

        [HttpPost("fetch-next-unique-month")]
        public async Task<IActionResult> FetchNextUniqueMonth()
        {
            try
            {
                await _marketDataService.FetchNextUniqueMonth();
                return Ok("Next unique month fetched and saved successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching unique month data.", details = ex.Message });
            }
        }

    }
}
