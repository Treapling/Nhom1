using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nhom1.Data;
using Nhom1.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Linq;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _secretKey = "SGU_TourGuide_SecretKey_2026_Secure_Super_Long_Key"; 

        public AuthController(AppDbContext context) { _context = context; }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == request.Password);
            if (user == null || !user.IsActive) return Unauthorized(new { message = "Sai tài khoản hoặc tài khoản bị khóa." });
            var token = GenerateJwtToken(user.Id.ToString(), user.Role, DateTime.UtcNow.AddDays(30));
            return Ok(new { Token = token, Role = user.Role, ShopName = user.ShopName });
        }

        [HttpPost("guest-free")]
        public IActionResult GetGuestFreeToken()
        {
            string sessionId = "Free_" + Guid.NewGuid().ToString().Substring(0, 8);
            var token = GenerateJwtToken(sessionId, "GuestFree", DateTime.UtcNow.AddYears(1));
            return Ok(new { Token = token, SessionId = sessionId, Role = "GuestFree", Message = "Kích hoạt gói Thường thành công." });
        }

        [HttpPost("guest-premium")]
        public IActionResult GetGuestPremiumToken()
        {
            string sessionId = "Prem_" + Guid.NewGuid().ToString().Substring(0, 8);
            var token = GenerateJwtToken(sessionId, "GuestPremium", DateTime.UtcNow.AddHours(24));
            return Ok(new { Token = token, SessionId = sessionId, Role = "GuestPremium", ExpireHours = 24, Message = "Kích hoạt gói Premium 24h thành công." });
        }

        private string GenerateJwtToken(string userId, string role, DateTime expires)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor {
                // ĐÃ SỬA: Ép cứng cả Claim chuẩn hóa và Claim thô để chống lỗi nuốt dữ liệu ID
                Subject = new ClaimsIdentity(new[] { 
                    new Claim(ClaimTypes.NameIdentifier, userId), 
                    new Claim(ClaimTypes.Role, role),
                    new Claim("role", role),
                    new Claim("sub", userId) 
                }),
                Expires = expires,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // ĐÃ BỔ SUNG: Lớp trung gian DTO để nhận dữ liệu đăng nhập
    public class LoginRequest 
    { 
        public string Username { get; set; } 
        public string Password { get; set; } 
    }
}