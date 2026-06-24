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

        [HttpGet("poi/{poiId}")]
        [Authorize(Roles = "Guest,Vendor,Admin")] 
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
            
            // Lệnh chuẩn để trả về lỗi 403 kèm nội dung văn bản
            if (poi == null) return StatusCode(403, "Bạn không có quyền thêm món cho địa điểm này (ID không thuộc quyền sở hữu của bạn).");

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();
            return Ok(menu);
        }
    }
}