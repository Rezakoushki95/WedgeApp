using backend.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardController(LeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpGet]
    public IActionResult GetLeaderboard(int page = 1, int pageSize = 10)
    {
        try
        {
            var (users, totalUsers) = _leaderboardService.GetLeaderboard(page, pageSize);
            return Ok(new
            {
                TotalUsers = totalUsers,
                Page = page,
                PageSize = pageSize,
                Users = users
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
