using Microsoft.AspNetCore.Mvc;
using backend.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using backend.DTOs;
using backend.Models;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
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

            return Ok("User registered successfully.");
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

            // Create a new TradingSession when the user starts a session
            var tradingSession = new TradingSession
            {
                UserId = user.Id,
                Instrument = "S&P 500",
                CurrentBarIndex = 0,
                HasOpenOrder = false,
                EntryPrice = null,
                CurrentProfitLoss = null,
                TotalProfitLoss = 0,
                TotalOrders = 0
            };
            _context.TradingSessions.Add(tradingSession);
            await _context.SaveChangesAsync();

            return Ok("Trading session started successfully.");
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
