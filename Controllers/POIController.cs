using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class POIController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Tiêm DbContext vào để tương tác với SQLite
        public POIController(AppDbContext context)
        {
            _context = context;
        }

        // API 1: Lấy danh sách toàn bộ POI (Dùng cho bản đồ hiển thị)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<POI>>> GetPOIs()
        {
            return await _context.POIs.ToListAsync();
        }

        // API 2: Thêm mới một POI (Dùng cho trang CMS Admin)
        [HttpPost]
        public async Task<ActionResult<POI>> PostPOI(POI pOI)
        {
            _context.POIs.Add(pOI);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPOI), new { id = pOI.Id }, pOI);
        }

        // API 3: Lấy chi tiết 1 POI
        [HttpGet("{id}")]
        public async Task<ActionResult<POI>> GetPOI(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);

            if (pOI == null)
            {
                return NotFound();
            }

            return pOI;
        }

        // API 4: Xóa một POI
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePOI(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);
            if (pOI == null)
            {
                return NotFound();
            }

            _context.POIs.Remove(pOI);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}