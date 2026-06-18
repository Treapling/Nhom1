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

        // API 1: Lấy danh sách toàn bộ POI kèm Audio đã được chuẩn hóa cấu trúc JSON
        [HttpGet]
        public async Task<IActionResult> GetPOIs()
        {
            var pois = await _context.POIs
                .Include(p => p.Audios)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    lat = p.Lat,
                    lng = p.Lng,
                    radius = p.Radius,
                    priority = p.Priority,
                    // Ép cấu hình chữ thường (camelCase) để khớp 100% với file admin.html phía Frontend
                    audios = p.Audios.Select(a => new {
                        id = a.Id,
                        filePath = a.FilePath,
                        language = a.Language,
                        poI_Id = a.POI_Id
                    }).ToList()
                })
                .ToListAsync();

            return Ok(pois);
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