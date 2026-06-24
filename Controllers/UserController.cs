using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nhom1.Data;
using Nhom1.Models;

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
            _context.Users.Add(user);
            _context.SaveChanges();
            
            return Ok(new { message = $"Đã tạo tài khoản Vendor '{user.Username}' thành công!" });
        }
    }
}