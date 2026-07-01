using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChartController : ControllerBase
{
    private readonly ChartService _chartService;

    public ChartController(ChartService chartService)
    {
        _chartService = chartService;
    }

    // Deal a random anonymized chart to trade.
    [HttpGet("deal")]
    public async Task<IActionResult> Deal()
    {
        var chart = await _chartService.DealRandomChart();
        if (chart == null)
        {
            return NotFound(new { message = "No chart data available. Fetch market data first." });
        }
        return Ok(chart);
    }

    // Re-deal a specific chart by id (e.g. to resume a session).
    [HttpGet("{chartId:int}")]
    public async Task<IActionResult> GetById(int chartId)
    {
        var chart = await _chartService.DealChart(chartId);
        if (chart == null)
        {
            return NotFound(new { message = $"Chart {chartId} not found." });
        }
        return Ok(chart);
    }
}
