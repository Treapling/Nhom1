using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
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

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPOIs()
        {
            var pois = await _context.POIs
                .Select(p => new {
                    id = p.Id, name = p.Name, description = p.Description, lat = p.Lat, lng = p.Lng,
                    radius = p.Radius, priority = p.Priority, approvalStatus = p.ApprovalStatus,
                    listenCount = p.TrackingLogs.Count(), 
                    audios = p.Audios.Select(a => new { id = a.Id, filePath = a.FilePath, language = a.Language, isPremium = a.IsPremium }).ToList()
                }).ToListAsync();
            return Ok(pois);
        }

        [HttpGet("vendor")]
        [Authorize(Roles = "Vendor")]
        public IActionResult GetVendorPOIs()
        {
            var vendorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (vendorIdClaim == null) return Unauthorized();
            int vendorId = int.Parse(vendorIdClaim);

            var pois = _context.POIs
                .Where(p => p.UserId == vendorId)
                .Select(p => new { id = p.Id, name = p.Name, approvalStatus = p.ApprovalStatus, listenCount = p.TrackingLogs.Count() })
                .ToList();
            return Ok(pois);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostPOI([FromBody] POI pOI)
        {
            if (User.IsInRole("Vendor"))
            {
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                var vendorInfo = await _context.Users.FindAsync(vendorId);
                var currentPoiCount = await _context.POIs.CountAsync(p => p.UserId == vendorId);

                if (currentPoiCount >= vendorInfo.MaxPOISlots)
                {
                    string errorMessage = vendorInfo.MaxPOISlots == 0 
                        ? "Bạn chưa có Slot địa điểm nào. Vui lòng thanh toán Mua thêm Slot trước khi đăng ký quán!" 
                        : $"Gian hàng của bạn đã đạt giới hạn {vendorInfo.MaxPOISlots} địa điểm. Vui lòng thanh toán nâng cấp Premium để mở rộng!";
                    return BadRequest(new { message = errorMessage });
                }

                pOI.UserId = vendorId;
                pOI.ApprovalStatus = 0; 
            }
            else { pOI.ApprovalStatus = 1; }

            _context.POIs.Add(pOI);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm địa điểm thành công!", id = pOI.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPOI(int id)
        {
            var pOI = await _context.POIs
                .Include(p => p.Audios)
                .FirstOrDefaultAsync(p => p.Id == id && p.ApprovalStatus == 1);
                
            if (pOI == null) return NotFound(new { message = "Địa điểm không tồn tại hoặc chưa được kiểm duyệt." });

            var log = new TrackingLog { POI_Id = id, EventType = "SCAN_QR", Timestamp = DateTime.UtcNow };
            _context.TrackingLogs.Add(log);
            await _context.SaveChangesAsync();

            var reviews = await _context.Reviews.Where(r => r.POI_Id == id).ToListAsync();
            double averageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;
            int totalListens = await _context.TrackingLogs.CountAsync(l => l.POI_Id == id);

            var poiDto = new {
                id = pOI.Id, name = pOI.Name, lat = pOI.Lat, lng = pOI.Lng, radius = pOI.Radius,
                listenCount = totalListens, ratingAvg = averageRating, ratingTotalCount = reviews.Count,
                // ĐÓNG GÓI ĐA NGÔN NGỮ
                descriptions = new {
                    vi = pOI.Description,
                    en = pOI.DescriptionEn,
                    zh = pOI.DescriptionZh,
                    ko = pOI.DescriptionKo,
                    ja = pOI.DescriptionJa
                },
                audios = pOI.Audios.Select(a => new { id = a.Id, filePath = a.FilePath, language = a.Language, isPremium = a.IsPremium }).ToList()
            };

            return Ok(poiDto);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Vendor")]
        public async Task<IActionResult> DeletePOI(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);
            if (pOI == null) return NotFound();
            if (User.IsInRole("Vendor"))
            {
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                if (pOI.UserId != vendorId) return Forbid();
            }
            _context.POIs.Remove(pOI);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignVendor(int id, [FromBody] AssignVendorDto request)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound(new { message = "Không tìm thấy địa điểm." });
            var vendor = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.VendorUsername && u.Role == "Vendor");
            if (vendor == null) return NotFound(new { message = "Không tìm thấy tài khoản." });
            poi.UserId = vendor.Id;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Thành công! Đã gán địa điểm '{poi.Name}' cho tài khoản '{vendor.Username}'." });
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePOI(int id, [FromBody] ApproveStatusDto request)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound();
            poi.ApprovalStatus = request.Status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công." });
        }
    }
    public class AssignVendorDto { public string VendorUsername { get; set; } }
    public class ApproveStatusDto { public int Status { get; set; } }
}