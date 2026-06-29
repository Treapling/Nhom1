using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly AppDbContext _context;
        public MenuController(AppDbContext context) { _context = context; }

        // Đã sửa Role để GuestFree và GuestPremium đều có thể đọc được thực đơn
        [HttpGet("poi/{poiId}")]
        [Authorize] 
        public async Task<IActionResult> GetMenusByPoi(int poiId)
        {
            var menus = await _context.Menus.Where(m => m.POI_Id == poiId).ToListAsync();
            return Ok(menus);
        }

        [HttpPost]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> AddMenuItem([FromBody] Menu menu)
        {
            var vendorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (vendorIdClaim == null) return Unauthorized();
            int vendorId = int.Parse(vendorIdClaim);

            var poi = await _context.POIs.FirstOrDefaultAsync(p => p.Id == menu.POI_Id && p.UserId == vendorId);
            
            if (poi == null) return StatusCode(403, "Bạn không có quyền thêm món cho địa điểm này.");

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();
            return Ok(menu);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> UpdateMenuItem(int id, [FromBody] Menu updatedMenu)
        {
            var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var menu = await _context.Menus.Include(m => m.POI).FirstOrDefaultAsync(m => m.Id == id);
            
            if (menu == null || menu.POI.UserId != vendorId) return StatusCode(403, "Không có quyền.");

            menu.ItemName = updatedMenu.ItemName;
            menu.Price = updatedMenu.Price;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Sửa món ăn thành công." });
        }

        // TÍNH NĂNG MỚI: XÓA MÓN ĂN CHO VENDOR
        [HttpDelete("{id}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var vendorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (vendorIdClaim == null) return Unauthorized();
            int vendorId = int.Parse(vendorIdClaim);

            var menu = await _context.Menus.Include(m => m.POI).FirstOrDefaultAsync(m => m.Id == id);
            if (menu == null) return NotFound();

            // Check quyền sở hữu
            if (menu.POI.UserId != vendorId) return StatusCode(403, "Bạn không có quyền xóa món này.");

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa món ăn thành công." });
        }
    }
}