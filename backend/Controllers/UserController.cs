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
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
        {
            if (string.IsNullOrWhiteSpace(registerDto.Username))
            {
                return BadRequest("Username cannot be empty.");
            }

            if (await UsernameExists(registerDto.Username))
            {
                return BadRequest("Username already exists.");
            }

            if (string.IsNullOrWhiteSpace(registerDto.Password))
            {
                return BadRequest("Password cannot be empty.");
            }

            var user = new User
            {
                Username = registerDto.Username,
                PasswordHash = HashPassword(registerDto.Password) // Hash the plain-text password
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully.", UserId = user.Id });
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

            return Ok(new { Message = "Login successful.", UserId = user.Id });
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
    }
}
