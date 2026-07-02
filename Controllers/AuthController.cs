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
    /// <summary>
    /// [CONTROLLER] Xác thực - Xử lý đăng nhập và cấp JWT token cho các loại người dùng
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _secretKey = "SGU_TourGuide_SecretKey_2026_Secure_Super_Long_Key"; // Khóa bí mật để ký JWT token

        public AuthController(AppDbContext context) { _context = context; }

        /// <summary>
        /// [POST /api/auth/login] - Đăng nhập bằng tài khoản (username + password)
        /// Kiểm tra thông tin đăng nhập, nếu đúng thì trả về JWT token + Role + ShopName
        /// Nếu sai hoặc tài khoản bị khóa (IsActive = false) => trả về 401 Unauthorized
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == request.Password);
            if (user == null || !user.IsActive) return Unauthorized(new { message = "Sai tài khoản hoặc tài khoản bị khóa." });
            var token = GenerateJwtToken(user.Id.ToString(), user.Role, DateTime.UtcNow.AddDays(30)); // Token hết hạn sau 30 ngày
            return Ok(new { Token = token, Role = user.Role, ShopName = user.ShopName });
        }

        /// <summary>
        /// [POST /api/auth/guest-free] - Cấp token miễn phí cho khách vãng lai (gói Free)
        /// Tạo session ID ngẫu nhiên, token có hạn 1 năm
        /// GuestFree chỉ được nghe audio thường, không được xem bình luận
        /// </summary>
        [HttpPost("guest-free")]
        public IActionResult GetGuestFreeToken()
        {
            string sessionId = "Free_" + Guid.NewGuid().ToString().Substring(0, 8);
            var token = GenerateJwtToken(sessionId, "GuestFree", DateTime.UtcNow.AddYears(1));
            return Ok(new { Token = token, SessionId = sessionId, Role = "GuestFree", Message = "Kích hoạt gói Thường thành công." });
        }

        /// <summary>
        /// [POST /api/auth/guest-premium] - Cấp token Premium cho khách vãng lai (gói VIP 24h)
        /// Tạo session ID ngẫu nhiên, token có hạn 24 giờ
        /// GuestPremium được nghe audio Premium và xem bình luận
        /// </summary>
        [HttpPost("guest-premium")]
        public IActionResult GetGuestPremiumToken()
        {
            string sessionId = "Prem_" + Guid.NewGuid().ToString().Substring(0, 8);
            var token = GenerateJwtToken(sessionId, "GuestPremium", DateTime.UtcNow.AddHours(24));
            return Ok(new { Token = token, SessionId = sessionId, Role = "GuestPremium", ExpireHours = 24, Message = "Kích hoạt gói Premium 24h thành công." });
        }

        /// <summary>
        /// [PRIVATE] Hàm tạo JWT token với các claims:
        /// - ClaimTypes.NameIdentifier: ID người dùng
        /// - ClaimTypes.Role + "role": Vai trò (Admin/Vendor/GuestFree/GuestPremium)
        /// - "sub": ID người dùng (dự phòng)
        /// </summary>
        private string GenerateJwtToken(string userId, string role, DateTime expires)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] { 
                    new Claim(ClaimTypes.NameIdentifier, userId), // Claim chuẩn cho User ID
                    new Claim(ClaimTypes.Role, role),            // Claim chuẩn cho Role
                    new Claim("role", role),                     // Claim thô (dự phòng khi claim chuẩn bị nuốt)
                    new Claim("sub", userId)                     // Claim thô cho User ID (dự phòng)
                }),
                Expires = expires,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    /// <summary>
    /// [DTO] Lớp trung gian nhận dữ liệu đăng nhập từ client
    /// Tránh lỗi validation tự động của C# trên model User
    /// </summary>
    public class LoginRequest 
    { 
        public string Username { get; set; } 
        public string Password { get; set; } 
    }
}