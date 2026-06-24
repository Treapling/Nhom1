using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // BỔ SUNG DÒNG NÀY CHỨA [Authorize]
using Nhom1.Data;
using Nhom1.Models;
using System.Linq;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class POIController : ControllerBase
    {
        private readonly AppDbContext _context;

        public POIController(AppDbContext context)
        {
            _context = context;
        }

        // API 1: Lấy danh sách toàn bộ POI (Dành cho Admin)
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
                    // FIX: Đếm số lượng dòng log thực tế trong bảng TrackingLogs
                    listenCount = p.TrackingLogs.Count(), 
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

        [HttpGet("vendor")]
        [Authorize(Roles = "Vendor")]
        public IActionResult GetVendorPOIs()
        {
            // Trích xuất ID của Vendor từ JWT Token
            var vendorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (vendorIdClaim == null) return Unauthorized();
            int vendorId = int.Parse(vendorIdClaim);

            var pois = _context.POIs
                .Where(p => p.UserId == vendorId) // Ép điều kiện Row-level Security
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    listenCount = p.TrackingLogs.Count()
                }).ToList();

            return Ok(pois);
        }

        // API 2: Thêm mới một POI
        [HttpPost]
        public async Task<ActionResult<POI>> PostPOI(POI pOI)
        {
            _context.POIs.Add(pOI);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPOI), new { id = pOI.Id }, pOI);
        }

        // API 3: Lấy chi tiết 1 POI (Dành cho chức năng Quét QR)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPOI(int id)
        {
            var pOI = await _context.POIs.Include(p => p.Audios).FirstOrDefaultAsync(p => p.Id == id);
            if (pOI == null) return NotFound();

            // 1. GHI LOG (DATA PIPELINE) THAY VÌ CỘNG ListenCount
            var log = new TrackingLog {
                POI_Id = id,
                EventType = "SCAN_QR",
                Timestamp = DateTime.UtcNow
            };
            _context.TrackingLogs.Add(log);
            await _context.SaveChangesAsync();

            // 2. QUERY ĐẾM SỐ LƯỢT NGHE TỪ BẢNG LOG ĐỂ TRẢ VỀ CHO DASHBOARD
            int totalListens = await _context.TrackingLogs.CountAsync(l => l.POI_Id == id);

            var poiDto = new {
                id = pOI.Id,
                name = pOI.Name,
                description = pOI.Description,
                lat = pOI.Lat,
                lng = pOI.Lng,
                radius = pOI.Radius,
                listenCount = totalListens, // Lấy số liệu đếm từ CSDL Log
                audioUrl = pOI.Audios.FirstOrDefault()?.FilePath 
            };

            return Ok(poiDto);
        }

        // API 4: Xóa một POI
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePOI(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);
            if (pOI == null) return NotFound();

            _context.POIs.Remove(pOI);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // --- DÁN THÊM API THỨ 5 NÀY VÀO ĐÂY ---
        // API 5: Gán quyền sở hữu địa điểm cho Chủ quán (Chỉ Admin được dùng)
        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignVendor(int id, [FromBody] AssignVendorDto request)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound(new { message = "Không tìm thấy địa điểm." });

            var vendor = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.VendorUsername && u.Role == "Vendor");
            if (vendor == null) return NotFound(new { message = "Không tìm thấy tài khoản Chủ quán (Vendor) này." });

            poi.UserId = vendor.Id;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Thành công! Đã gán địa điểm '{poi.Name}' cho tài khoản '{vendor.Username}'." });
        }
        // -------------------------------------
    }

    public class AssignVendorDto
    {
        public string VendorUsername { get; set; }
    }
}