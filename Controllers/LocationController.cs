using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Nhom1.Data;
using Nhom1.Models;
using Nhom1.Services;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;

namespace Nhom1.Controllers
{
    /// <summary>
    /// [CONTROLLER] Vị trí địa lý (Geolocation) - Kiểm tra người dùng có nằm trong vùng geofence của POI không
    /// Sử dụng GeofenceService để tính khoảng cách và kích hoạt sự kiện GPS_TRIGGER
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly GeofenceService _geofenceService;

        public LocationController(AppDbContext context, GeofenceService geofenceService)
        {
            _context = context;
            _geofenceService = geofenceService;
        }

        /// <summary>
        /// [GET /api/location/check?lat=&lng=] - [Auth] Kiểm tra vị trí GPS của người dùng
        /// 1. Lấy danh sách tất cả POI đã được duyệt (ApprovalStatus = 1)
        /// 2. Dùng GeofenceService tính khoảng cách từ user đến từng POI
        /// 3. Nếu user nằm trong bán kính (Radius) của POI => kích hoạt (triggered = true)
        /// 4. Ghi nhận sự kiện GPS_TRIGGER vào TrackingLogs cho mỗi POI bị kích hoạt
        /// 5. Trả về danh sách POI ID đã kích hoạt
        /// </summary>
        [HttpGet("check")]
        [Authorize]
        public async Task<IActionResult> CheckLocation([FromQuery] double lat, [FromQuery] double lng)
        {
            var allPOIs = await _context.POIs.Where(p => p.ApprovalStatus == 1).ToListAsync();
            var triggeredPois = _geofenceService.CheckTriggeredPOIs(lat, lng, allPOIs);

            if (triggeredPois.Any())
            {
                // Lấy session token từ JWT để ghi nhận GPS trigger
                var sessionToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                   ?? User.FindFirst("sub")?.Value 
                                   ?? "Anon_GPS_" + Guid.NewGuid().ToString().Substring(0,4);

                // Ghi log GPS_TRIGGER cho mỗi POI bị kích hoạt
                foreach (var poi in triggeredPois)
                {
                    var log = new TrackingLog 
                    { 
                        POI_Id = poi.Id, 
                        EventType = "GPS_TRIGGER", 
                        Timestamp = DateTime.UtcNow, 
                        SessionToken = sessionToken 
                    };
                    _context.TrackingLogs.Add(log);
                }
                await _context.SaveChangesAsync();

                var ids = triggeredPois.Select(p => p.Id).ToList();
                return Ok(new { triggered = true, poiIds = ids });
            }

            // Không nằm trong vùng geofence nào
            return Ok(new { triggered = false, poiIds = new int[] {} });
        }
    }
}