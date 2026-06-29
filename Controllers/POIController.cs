using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
using Nhom1.Data;
using Nhom1.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class POIController : ControllerBase
    {
        private readonly AppDbContext _context;
        public POIController(AppDbContext context) { _context = context; }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPOIs()
        {
            var pois = await _context.POIs
                .Select(p => new {
                    id = p.Id, name = p.Name, description = p.Description, lat = p.Lat, lng = p.Lng,
                    radius = p.Radius, priority = p.Priority, approvalStatus = p.ApprovalStatus,
                    listenCount = _context.TrackingLogs.Count(t => t.POI_Id == p.Id), 
                    audios = p.Audios.Select(a => new { id = a.Id, filePath = a.FilePath, language = a.Language, isPremium = a.IsPremium }).ToList()
                }).ToListAsync();
            return Ok(pois);
        }

        [HttpGet("vendor")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetVendorPOIs()
        {
            var vendorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (vendorIdClaim == null) return Unauthorized();
            int vendorId = int.Parse(vendorIdClaim);

            var pois = await _context.POIs
                .Where(p => p.UserId == vendorId)
                .Select(p => new { 
                    id = p.Id, name = p.Name, description = p.Description, lat = p.Lat, lng = p.Lng, approvalStatus = p.ApprovalStatus, 
                    listenCount = _context.TrackingLogs.Count(t => t.POI_Id == p.Id),
                    ratingAvg = _context.Reviews.Any(r => r.POI_Id == p.Id) ? Math.Round(_context.Reviews.Where(r => r.POI_Id == p.Id).Average(r => (double)r.Rating), 1) : 0,
                    ratingTotalCount = _context.Reviews.Count(r => r.POI_Id == p.Id)
                }).ToListAsync();
            return Ok(pois);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> UpdatePOI(int id, [FromBody] POI updatedPOI)
        {
            var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var poi = await _context.POIs.FirstOrDefaultAsync(p => p.Id == id && p.UserId == vendorId);
            if (poi == null) return NotFound(new { message = "Không tìm thấy địa điểm hoặc không có quyền." });

            poi.Name = updatedPOI.Name;
            poi.Description = updatedPOI.Description;
            poi.Lat = updatedPOI.Lat;
            poi.Lng = updatedPOI.Lng;
            poi.Radius = 10;
            poi.Priority = 1;
            poi.ApprovalStatus = 0; 

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công. Đang chờ Admin duyệt lại." });
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
                pOI.Radius = 10;
                pOI.Priority = 1;
            }
            else { pOI.ApprovalStatus = 1; }

            _context.POIs.Add(pOI);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm địa điểm thành công!", id = pOI.Id });
        }

        [HttpGet("{id}")]
        [Authorize] // ĐÃ CẬP NHẬT: Kích hoạt định danh bảo mật cho API xem chi tiết
        public async Task<IActionResult> GetPOI(int id)
        {
            var pOI = await _context.POIs.Include(p => p.Audios).FirstOrDefaultAsync(p => p.Id == id && p.ApprovalStatus == 1);
            if (pOI == null) return NotFound(new { message = "Địa điểm không tồn tại hoặc chưa được kiểm duyệt." });

            // SỬA LỖI ĐẾM ONLINE: Trích xuất Session ID duy nhất của thiết bị từ chuỗi JWT Claims
            var sessionToken = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Anonymous_QR";

            var log = new TrackingLog { POI_Id = id, EventType = "SCAN_QR", Timestamp = DateTime.UtcNow, SessionToken = sessionToken };
            _context.TrackingLogs.Add(log);
            await _context.SaveChangesAsync();

            var reviews = await _context.Reviews.Where(r => r.POI_Id == id).ToListAsync();
            double averageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;
            int totalListens = await _context.TrackingLogs.CountAsync(l => l.POI_Id == id);

            var poiDto = new {
                id = pOI.Id, name = pOI.Name, lat = pOI.Lat, lng = pOI.Lng, radius = pOI.Radius,
                listenCount = totalListens, ratingAvg = averageRating, ratingTotalCount = reviews.Count,
                descriptions = new { vi = pOI.Description, en = pOI.DescriptionEn, zh = pOI.DescriptionZh, ko = pOI.DescriptionKo, ja = pOI.DescriptionJa },
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

            _context.Audios.RemoveRange(_context.Audios.Where(a => a.POI_Id == id));
            _context.Menus.RemoveRange(_context.Menus.Where(m => m.POI_Id == id));
            _context.Reviews.RemoveRange(_context.Reviews.Where(r => r.POI_Id == id));
            _context.TrackingLogs.RemoveRange(_context.TrackingLogs.Where(t => t.POI_Id == id));

            _context.POIs.Remove(pOI);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // [HttpPut("{id}/assign")]
        // [Authorize(Roles = "Admin")]
        // public async Task<IActionResult> AssignVendor(int id, [FromBody] AssignVendorDto request)
        // {
        //     var poi = await _context.POIs.FindAsync(id);
        //     if (poi == null) return NotFound(new { message = "Không tìm thấy địa điểm." });
        //     var vendor = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.VendorUsername && u.Role == "Vendor");
        //     if (vendor == null) return NotFound(new { message = "Không tìm thấy tài khoản." });
        //     poi.UserId = vendor.Id;
        //     await _context.SaveChangesAsync();
        //     return Ok(new { message = $"Thành công! Đã gán địa điểm '{poi.Name}' cho tài khoản '{vendor.Username}'." });
        // }

        // [HttpPut("{id}/approve")]
        // [Authorize(Roles = "Admin")]
        // public async Task<IActionResult> ApprovePOI(int id, [FromBody] ApproveStatusDto request)
        // {
        //     var poi = await _context.POIs.FindAsync(id);
        //     if (poi == null) return NotFound();
        //     poi.ApprovalStatus = request.Status;
        //     await _context.SaveChangesAsync();
        //     return Ok(new { message = "Cập nhật thành công." });
        // }
    }
}