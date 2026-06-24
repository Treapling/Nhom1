using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nhom1.Data;
using Nhom1.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        // Key này phải trùng khớp 100% với key trong Program.cs
        private readonly string _secretKey = "SGU_TourGuide_SecretKey_2026_Secure_Super_Long_Key"; 

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // 1. API Login cho Chủ Quán / Admin
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Trong thực tế PasswordHash phải được mã hóa BCrypt, ở đây demo dùng plaintext so sánh
            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == request.Password);
            if (user == null || !user.IsActive) return Unauthorized("Sai tài khoản hoặc tài khoản bị khóa.");

            var token = GenerateJwtToken(user.Id.ToString(), user.Role, DateTime.UtcNow.AddDays(30));
            return Ok(new { Token = token, Role = user.Role, ShopName = user.ShopName });
        }

        // 2. API Cấp Token 24h cho Khách Du lịch sau khi thanh toán QR thành công
        [HttpPost("guest-token")]
        public IActionResult GetGuestToken()
        {
            string guestSessionId = "Guest_" + Guid.NewGuid().ToString().Substring(0, 8);
            var token = GenerateJwtToken(guestSessionId, "Guest", DateTime.UtcNow.AddHours(24));
            
            return Ok(new { 
                Token = token, 
                SessionId = guestSessionId,
                ExpireHours = 24,
                Message = "Thanh toán thành công. Dịch vụ AI Tour Guide đã mở khóa trong 24 giờ."
            });
        }

        private string GenerateJwtToken(string userId, string role, DateTime expires)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim("role", role)
                }),
                Expires = expires,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}