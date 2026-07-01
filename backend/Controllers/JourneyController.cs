using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JourneyController : ControllerBase
{
    private readonly JourneyService _journeyService;

    public JourneyController(JourneyService journeyService)
    {
        _journeyService = journeyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetJourneys([FromQuery] int userId = 1, [FromQuery] bool includeArchived = false)
    {
        return Ok(await _journeyService.GetJourneys(userId, includeArchived));
    }

    [HttpGet("{journeyId:int}/stats")]
    public async Task<IActionResult> GetStats(int journeyId)
    {
        var stats = await _journeyService.GetStats(journeyId);
        return stats == null ? NotFound(new { message = $"Journey {journeyId} not found." }) : Ok(stats);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJourneyDTO dto)
    {
        var journey = await _journeyService.CreateJourney(dto);
        return Ok(journey);
    }

    [HttpPut("{journeyId:int}/rename")]
    public async Task<IActionResult> Rename(int journeyId, [FromBody] RenameJourneyDTO dto)
    {
        var journey = await _journeyService.RenameJourney(journeyId, dto.Name);
        return journey == null ? NotFound(new { message = $"Journey {journeyId} not found." }) : Ok(journey);
    }

    [HttpPut("{journeyId:int}/archive")]
    public async Task<IActionResult> Archive(int journeyId, [FromQuery] bool archived = true)
    {
        var journey = await _journeyService.SetArchived(journeyId, archived);
        return journey == null ? NotFound(new { message = $"Journey {journeyId} not found." }) : Ok(journey);
    }

    // Record a completed trade against a journey. R is recomputed server-side.
    [HttpPost("trade")]
    public async Task<IActionResult> SubmitTrade([FromBody] SubmitTradeDTO dto)
    {
        try
        {
            return Ok(await _journeyService.SubmitTrade(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class RenameJourneyDTO
{
    public string Name { get; set; } = string.Empty;
}
