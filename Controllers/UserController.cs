using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserController(AppDbContext context) { _context = context; }

        [HttpPost("vendor")]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateVendor([FromBody] CreateVendorRequest request)
        {
            // Kiểm tra trùng lặp tài khoản
            if (_context.Users.Any(u => u.Username == request.Username))
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại trong hệ thống." });
            
            // Khởi tạo thực thể User mới từ dữ liệu DTO để né bộ lọc Validation tự động của C#
            var user = new User
            {
                Username = request.Username,
                PasswordHash = request.PasswordHash,
                Role = "Vendor",
                IsActive = true,
                MaxPOISlots = 0,
                ShopName = "Chưa cập nhật"
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            
            return Ok(new { message = $"Đã tạo tài khoản Vendor '{user.Username}' thành công!" });
        }

        [HttpPost("buy-slot")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> BuySlot()
        {
            var vendorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (vendorIdClaim == null) return Unauthorized();
            int vendorId = int.Parse(vendorIdClaim);

            var vendor = await _context.Users.FindAsync(vendorId);
            if (vendor == null) return NotFound();

            vendor.MaxPOISlots += 1;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Thanh toán thành công! Hệ thống đã tự động cộng thêm 1 Slot đăng ký quán cho bạn." 
            });
        }
    }

    // LỚP TRUNG GIAN (DTO) GIÚP GIẢI QUYẾT TRIỆT ĐỂ LỖI VALIDATION FIELD REQUIRED
    public class CreateVendorRequest
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
    }
}