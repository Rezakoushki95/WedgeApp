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
        public async Task<IActionResult> GetSession([FromQuery] int userId, [FromQuery] string instrument = "S&P 500")
        {
            // Validate inputs
            if (userId <= 0)
            {
                return BadRequest(new { message = "Invalid UserId. UserId must be greater than 0." });
            }

            if (string.IsNullOrWhiteSpace(instrument))
            {
                return BadRequest(new { message = "Instrument cannot be empty." });
            }

            try
            {
                // Fetch the session
                var session = await _tradingSessionService.GetSession(userId, instrument);
                if (session == null)
                {
                    return NotFound(new { message = $"No trading session found for user {userId} and instrument '{instrument}'." });
                }

                // Map to DTO
                var response = MapToDto(session);

                return Ok(response); // Return the DTO
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: ex.Message,
                    title: "An error occurred while fetching the session.",
                    statusCode: 500
                );
            }
        }

        // Helper method for mapping
        private static TradingSessionResponseDTO MapToDto(TradingSession session)
        {
            return new TradingSessionResponseDTO
            {
                SessionId = session.Id,
                Instrument = session.Instrument,
                TradingDay = session.TradingDay,
                CurrentBarIndex = session.CurrentBarIndex,
                HasOpenOrder = session.HasOpenOrder,
                EntryPrice = session.EntryPrice,
                TotalProfitLoss = session.TotalProfitLoss,
                TotalOrders = session.TotalOrders
            };
        }



        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDTO createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var session = await _tradingSessionService.CreateSession(createDto.UserId);

                var response = new TradingSessionResponseDTO
                {
                    SessionId = session.Id,
                    Instrument = session.Instrument,
                    TradingDay = session.TradingDay,
                    CurrentBarIndex = session.CurrentBarIndex,
                    HasOpenOrder = session.HasOpenOrder,
                    EntryPrice = session.EntryPrice,
                    TotalProfitLoss = session.TotalProfitLoss,
                    TotalOrders = session.TotalOrders
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPut("update-session")]
        public async Task<IActionResult> UpdateSession([FromBody] UpdateSessionDTO updateDto)
        {
            // Validate the input early
            if (updateDto.SessionId <= 0)
            {
                return BadRequest(new { message = "SessionId must be greater than 0." });
            }

            try
            {
                var updatedSession = await _tradingSessionService.UpdateSession(updateDto);
                return Ok(updatedSession); // Returns the response DTO
            }
            catch (KeyNotFoundException ex)
            {
                // Handle case where session is not found
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log unexpected errors (optional)
                Console.WriteLine($"Error: {ex.Message}");
                return Problem("An unexpected error occurred while updating the session.");
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
