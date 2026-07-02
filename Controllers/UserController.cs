using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Nhom1.Controllers
{
    /// <summary>
    /// [CONTROLLER] Quản lý Người dùng - Admin tạo tài khoản Vendor, Vendor mua thêm slot địa điểm
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserController(AppDbContext context) { _context = context; }

        /// <summary>
        /// [POST /api/user/vendor] - [Admin] Tạo tài khoản Vendor mới
        /// Kiểm tra trùng username, tạo User với Role = "Vendor", MaxPOISlots = 0 (chưa có slot)
        /// </summary>
        [HttpPost("vendor")]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateVendor([FromBody] CreateVendorRequest request)
        {
            // Kiểm tra trùng lặp tài khoản
            if (_context.Users.Any(u => u.Username == request.Username))
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại trong hệ thống." });

            // Tạo User mới từ DTO để tránh lỗi validation tự động của C#
            var user = new User
            {
                Username = request.Username,
                PasswordHash = request.PasswordHash,
                Role = "Vendor",
                IsActive = true,
                MaxPOISlots = 0, // Mặc định 0 slot => phải mua mới đăng ký được quán
                ShopName = "Chưa cập nhật"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = $"Đã tạo tài khoản Vendor '{user.Username}' thành công!" });
        }

        /// <summary>
        /// [POST /api/user/buy-slot] - [Vendor] Mua thêm slot đăng ký địa điểm
        /// Tăng MaxPOISlots lên 1 (mô phỏng thanh toán, chưa tích hợp cổng thanh toán thật)
        /// </summary>
        [HttpPost("buy-slot")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> BuySlot()
        {
            var vendorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (vendorIdClaim == null) return Unauthorized();
            int vendorId = int.Parse(vendorIdClaim);

            var vendor = await _context.Users.FindAsync(vendorId);
            if (vendor == null) return NotFound();

            vendor.MaxPOISlots += 1; // Cộng thêm 1 slot
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Thanh toán thành công! Hệ thống đã tự động cộng thêm 1 Slot đăng ký quán cho bạn." 
            });
        }
    }

    /// <summary>
    /// [DTO] Lớp trung gian nhận dữ liệu tạo Vendor từ Admin
    /// Tránh lỗi validation Required tự động trên model User
    /// </summary>
    public class CreateVendorRequest
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
    }
}