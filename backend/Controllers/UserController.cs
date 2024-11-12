using Microsoft.AspNetCore.Mvc;
using backend.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using backend.DTOs;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TradingSessionService _tradingSessionService;

        public UserController(AppDbContext context, TradingSessionService tradingSessionService)
        {
            _context = context;
            _tradingSessionService = tradingSessionService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                return BadRequest("Username cannot be empty.");
            }

            if (await UsernameExists(user.Username))
            {
                return BadRequest("Username already exists.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return BadRequest("Password cannot be empty.");
            }

            user.PasswordHash = HashPassword(user.PasswordHash);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Start a new trading session for the user
            var newSession = await _tradingSessionService.StartNewSession(user.Id);

            return Ok(new { Message = "User registered and session started successfully.", SessionId = newSession.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO login)
        {
            if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest("Username and password are required.");
            }

            // Find the user by username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == login.Username);
            if (user == null)
            {
                return Unauthorized("Invalid username");
            }

            // Hash the password provided and compare with the stored hash
            var hashedPassword = HashPassword(login.Password);
            if (user.PasswordHash != hashedPassword)
            {
                return Unauthorized("Invalid password.");
            }

            return Ok("Login successful.");
        }

        private async Task<bool> UsernameExists(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        [HttpPost("start-session")]
        public async Task<IActionResult> StartTradingSession(int userId)
        {
            // Check if the user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Start a new trading session using the service
            var newSession = await _tradingSessionService.StartNewSession(user.Id);

            return Ok(new { Message = "Session started successfully.", SessionId = newSession.Id });
        }


        [HttpGet("trading-session")]
        public async Task<IActionResult> GetTradingSession(int userId)
        {
            var tradingSession = await _context.TradingSessions.FirstOrDefaultAsync(ts => ts.UserId == userId);
            if (tradingSession == null)
            {
                return NotFound("No trading session found for this user.");
            }
            return Ok(tradingSession);
        }
    }
}
