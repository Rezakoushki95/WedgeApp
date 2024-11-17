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

        [HttpPut("update-session")]
        public async Task<IActionResult> UpdateSession([FromBody] UpdateSessionDto updateDto)
        {
            try
            {
                await _tradingSessionService.UpdateSession(
                    updateDto.SessionId,
                    updateDto.CurrentBarIndex,
                    updateDto.HasOpenOrder,
                    updateDto.EntryPrice,
                    updateDto.TotalProfitLoss,
                    updateDto.TotalOrders
                );
                return Ok(new { message = "Session updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("get-session")]
        public async Task<IActionResult> GetOrStartSession(int userId)
        {
            try
            {
                var session = await _tradingSessionService.GetOrStartSession(userId);
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
