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
            try
            {
                var unaccessedDay = await _marketDataService.GetUnaccessedDay(userId);

                if (unaccessedDay == null)
                {
                    return NotFound("No unaccessed day available for this user.");
                }

                return Ok(unaccessedDay);
            }
            catch (Exception ex)
            {
                // Log error (optional)
                return StatusCode(500, new { message = "An error occurred while fetching an unaccessed day.", details = ex.Message });
            }
        }

        [HttpPost("fetch-data")]
        public async Task<IActionResult> FetchAndSaveData()
        {
            try
            {
                await _marketDataService.FetchAndSaveMonthlyData();
                return Ok("Data fetched and saved successfully.");
            }
            catch (Exception ex)
            {
                // Log error (optional)
                return StatusCode(500, new { message = "An error occurred while fetching data.", details = ex.Message });
            }
        }
    }
}
