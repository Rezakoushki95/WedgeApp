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

        [HttpGet("get-session")]
        public async Task<IActionResult> GetSession(int userId)
        {
            try
            {
                var session = await _tradingSessionService.GetSession(userId);
                if (session == null)
                {
                    return NotFound(new { message = "No trading session found for this user." });
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("start-session")]
        public async Task<IActionResult> StartSession(int userId)
        {
            try
            {
                var session = await _tradingSessionService.StartSession(userId);
                return Ok(new { message = "New trading session started successfully.", session });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                return Ok(new { message = "Session updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
