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
        [Authorize] // ĐĂ SỬA: Đồng bộ bộ lọc danh tính GPS
        public async Task<IActionResult> CheckLocation([FromQuery] double lat, [FromQuery] double lng)
        {
            var allPOIs = await _context.POIs.Where(p => p.ApprovalStatus == 1).ToListAsync();
            var triggeredPois = _geofenceService.CheckTriggeredPOIs(lat, lng, allPOIs);

            if (triggeredPois.Any())
            {
                // FIX: Trích xuất chéo để lưu vết User thiết bị thứ 2, 3 lên biểu đồ CMS
                var sessionToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                   ?? User.FindFirst("sub")?.Value 
                                   ?? "Anon_GPS_" + Guid.NewGuid().ToString().Substring(0,4);

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

            return Ok(new { triggered = false, poiIds = new int[] {} });
        }
    }
}