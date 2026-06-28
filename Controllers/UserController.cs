using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;
using System.Threading.Tasks;

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
        public IActionResult CreateVendor([FromBody] User user)
        {
            if (_context.Users.Any(u => u.Username == user.Username))
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại trong hệ thống." });
            
            user.Role = "Vendor";
            user.IsActive = true;
            user.MaxPOISlots = 0; 
            
            // Xử lý mặc định Tên Shop vì Admin không còn nhập trường này nữa
            if (string.IsNullOrEmpty(user.ShopName)) 
                user.ShopName = "Chưa cập nhật";

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
}