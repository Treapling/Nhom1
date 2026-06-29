using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Nhom1.Data;
using Nhom1.Models;
using Nhom1.Services;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Nhom1.Controllers
{
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

        [HttpGet("check")]
        [Authorize] // ĐA CẬP NHẬT: Đảm bảo luồng kiểm tra GPS được định danh danh tính thiết bị
        public async Task<IActionResult> CheckLocation([FromQuery] double lat, [FromQuery] double lng)
        {
            var allPOIs = await _context.POIs.Where(p => p.ApprovalStatus == 1).ToListAsync();
            var triggeredPois = _geofenceService.CheckTriggeredPOIs(lat, lng, allPOIs);

            if (triggeredPois.Any())
            {
                // Trích xuất mã phiên thiết bị để phục vụ logic đếm số người Online thời gian thực
                var sessionToken = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Anonymous_GPS";

                foreach (var poi in triggeredPois)
                {
                    // Ghi nhận lịch sử di chuyển thụ động vào cơ sở dữ liệu
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

            return Ok(new { triggered = false, poiIds = new int[] {} });
        }
    }
}