using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradingSessionController : ControllerBase
    {
        private readonly TradingSessionService _tradingSessionService;

        public TradingSessionController(TradingSessionService tradingSessionService)
        {
            _tradingSessionService = tradingSessionService;
        }

        [HttpGet("active")]
        public async Task<ActionResult<TradingSession?>> GetActiveSession(int userId, string instrument = "S&P 500")
        {
            var session = await _tradingSessionService.GetActiveSession(userId, instrument);

            if (session == null)
            {
                return NotFound();
            }

            return Ok(session);
        }

        [HttpPost("close")]
        public async Task<IActionResult> CloseSession([FromQuery] int sessionId)
        {
            try
            {
                await _tradingSessionService.CloseSession(sessionId);
                return Ok(new { message = "Session closes successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
