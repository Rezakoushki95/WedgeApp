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
        public async Task<IActionResult> GetSession(int userId, string instrument)
        {
            try
            {
                var session = await _tradingSessionService.GetSession(userId, instrument);
                if (session == null)
                {
                    return NotFound(new { message = $"No trading session found for user {userId} and instrument {instrument}." });
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession(int userId)
        {
            try
            {
                var session = await _tradingSessionService.CreateSession(userId);
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

        [HttpPost("complete-day")]
        public async Task<IActionResult> CompleteDay(int sessionId)
        {
            try
            {
                await _tradingSessionService.CompleteDay(sessionId);
                return Ok(new { message = "Trading day completed and session reset for next day." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("get-bars")]
        public async Task<IActionResult> GetBars(int sessionId)
        {
            try
            {
                var bars = await _tradingSessionService.GetBarsForSession(sessionId);
                return Ok(bars);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
