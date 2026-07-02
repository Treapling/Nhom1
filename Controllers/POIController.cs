using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
using Nhom1.Data;
using Nhom1.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;

namespace Nhom1.Controllers
{
    /// <summary>
    /// [CONTROLLER] Quản lý Địa điểm (POI) - CRUD địa điểm du lịch/quán ăn
    /// Phân quyền: Admin xem tất cả, Vendor chỉ xử lý POI của mình
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class POIController : ControllerBase
    {
        private readonly AppDbContext _context;
        public POIController(AppDbContext context) { _context = context; }

        /// <summary>
        /// [GET /api/poi/admin] - [Admin] Lấy danh sách TẤT CẢ địa điểm trong hệ thống
        /// Bao gồm: id, tên, mô tả, tọa độ, bán kính, độ ưu tiên, trạng thái duyệt,
        /// số lượt nghe (đếm từ TrackingLogs), danh sách audio kèm ngôn ngữ
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPOIs()
        {
            var pois = await _context.POIs
                .Select(p => new {
                    id = p.Id, name = p.Name, description = p.Description, lat = p.Lat, lng = p.Lng,
                    radius = p.Radius, priority = p.Priority, approvalStatus = p.ApprovalStatus,
                    listenCount = _context.TrackingLogs.Count(t => t.POI_Id == p.Id), // Đếm tổng lượt tương tác
                    audios = p.Audios.Select(a => new { id = a.Id, filePath = a.FilePath, language = a.Language, isPremium = a.IsPremium }).ToList()
                }).ToListAsync();
            return Ok(pois);
        }

        /// <summary>
        /// [GET /api/poi/vendor] - [Vendor] Lấy danh sách địa điểm CỦA RIÊNG Vendor đang đăng nhập
        /// Lọc theo UserId = VendorId (lấy từ JWT token)
        /// Bao gồm thêm: điểm đánh giá trung bình, tổng số đánh giá
        /// </summary>
        [HttpGet("vendor")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetVendorPOIs()
        {
            var vendorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
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

        /// <summary>
        /// [PUT /api/poi/{id}] - [Vendor] Cập nhật thông tin địa điểm
        /// Kiểm tra quyền sở hữu: chỉ Vendor sở hữu POI mới được sửa
        /// Sau khi sửa => ApprovalStatus = 0 (chờ Admin duyệt lại)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> UpdatePOI(int id, [FromBody] POI updatedPOI)
        {
            var vendorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            var vendorId = int.Parse(vendorIdClaim);
            var poi = await _context.POIs.FirstOrDefaultAsync(p => p.Id == id && p.UserId == vendorId);
            if (poi == null) return NotFound(new { message = "Không tìm thấy địa điểm hoặc không có quyền." });

            poi.Name = updatedPOI.Name;
            poi.Description = updatedPOI.Description;
            poi.Lat = updatedPOI.Lat;
            poi.Lng = updatedPOI.Lng;
            poi.Radius = 10;  // Fix cứng bán kính = 10m
            poi.Priority = 1; // Fix cứng độ ưu tiên = 1
            poi.ApprovalStatus = 0; // Reset về chờ duyệt

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công. Đang chờ Admin duyệt lại." });
        }

        /// <summary>
        /// [POST /api/poi] - [Auth] Thêm địa điểm mới
        /// - Nếu là Vendor: kiểm tra số lượng slot còn trống (MaxPOISlots), gán UserId, đặt ApprovalStatus = 0
        /// - Nếu là Admin hoặc khác: tự động duyệt (ApprovalStatus = 1)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostPOI([FromBody] POI pOI)
        {
            if (User.IsInRole("Vendor") || User.HasClaim("role", "Vendor"))
            {
                var vendorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                var vendorId = int.Parse(vendorIdClaim);
                var vendorInfo = await _context.Users.FindAsync(vendorId);
                var currentPoiCount = await _context.POIs.CountAsync(p => p.UserId == vendorId);

                // Kiểm tra giới hạn slot
                if (currentPoiCount >= vendorInfo.MaxPOISlots)
                {
                    string errorMessage = vendorInfo.MaxPOISlots == 0 
                        ? "Bạn chưa có Slot địa điểm nào. Vui lòng thanh toán Mua thêm Slot trước khi đăng ký quán!" 
                        : $"Gian hàng của bạn đã đạt giới hạn {vendorInfo.MaxPOISlots} địa điểm. Vui lòng thanh toán nâng cấp Premium để mở rộng!";
                    return BadRequest(new { message = errorMessage });
                }

                pOI.UserId = vendorId;
                pOI.ApprovalStatus = 0; // Chờ Admin duyệt
                pOI.Radius = 10;
                pOI.Priority = 1;
            }
            else { pOI.ApprovalStatus = 1; } // Admin tự động duyệt

            _context.POIs.Add(pOI);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm địa điểm thành công!", id = pOI.Id });
        }

        /// <summary>
        /// [GET /api/poi/{id}] - [Auth] Xem chi tiết 1 địa điểm (chỉ lấy POI đã được duyệt)
        /// Tự động ghi nhận 1 lần SCAN_QR vào TrackingLogs khi có người xem
        /// Trả về: thông tin POI, mô tả đa ngôn ngữ, danh sách audio, điểm đánh giá, tổng lượt nghe
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPOI(int id)
        {
            var pOI = await _context.POIs.Include(p => p.Audios).FirstOrDefaultAsync(p => p.Id == id && p.ApprovalStatus == 1);
            if (pOI == null) return NotFound(new { message = "Địa điểm không tồn tại hoặc chưa được kiểm duyệt." });

            // Lấy session token từ JWT để ghi nhận lượt quét QR
            var sessionToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                               ?? User.FindFirst("sub")?.Value 
                               ?? "Anon_QR_" + Guid.NewGuid().ToString().Substring(0,4);

            // Ghi log sự kiện SCAN_QR
            var log = new TrackingLog { POI_Id = id, EventType = "SCAN_QR", Timestamp = DateTime.UtcNow, SessionToken = sessionToken };
            _context.TrackingLogs.Add(log);
            await _context.SaveChangesAsync();

            // Tính điểm đánh giá trung bình
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
    }
}