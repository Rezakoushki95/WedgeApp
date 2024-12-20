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

                // Map the model to the response DTO
                var response = new TradingSessionResponseDto
                {
                    SessionId = session.Id, // Map Id to SessionId
                    Instrument = session.Instrument,
                    TradingDay = session.TradingDay,
                    CurrentBarIndex = session.CurrentBarIndex,
                    HasOpenOrder = session.HasOpenOrder,
                    EntryPrice = session.EntryPrice,
                    TotalProfitLoss = session.TotalProfitLoss,
                    TotalOrders = session.TotalOrders
                };

                return Ok(response); // Return the DTO
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
                var updatedSession = await _tradingSessionService.UpdateSession(updateDto);
                return Ok(updatedSession); // Returns the response DTO
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
