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
