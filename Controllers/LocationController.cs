using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;
using Nhom1.Services;

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
        public async Task<IActionResult> CheckLocation([FromQuery] double lat, [FromQuery] double lng)
        {
            var allPOIs = await _context.POIs
                .Include(p => p.Audios) 
                .ToListAsync();

            var triggeredPoi = _geofenceService.CheckTriggeredPOI(lat, lng, allPOIs);

            if (triggeredPoi != null)
            {
                // 1. GHI LOG SỰ KIỆN GPS TRIGGER VÀO DATA PIPELINE
                var log = new TrackingLog {
                    POI_Id = triggeredPoi.Id,
                    EventType = "GPS_TRIGGER",
                    Timestamp = DateTime.UtcNow
                };
                _context.TrackingLogs.Add(log);
                await _context.SaveChangesAsync();

                // 2. ĐẾM LẠI SỐ LOG ĐỂ CẬP NHẬT CHART
                int totalListens = await _context.TrackingLogs.CountAsync(l => l.POI_Id == triggeredPoi.Id);

                var audioTrack = triggeredPoi.Audios?.FirstOrDefault();
                string audioPath = audioTrack != null ? audioTrack.FilePath : "default_audio.mp3"; 

                return Ok(new {
                    triggered = true,
                    poi = new {
                        id = triggeredPoi.Id,
                        name = triggeredPoi.Name,
                        description = triggeredPoi.Description,
                        lat = triggeredPoi.Lat,
                        lng = triggeredPoi.Lng,
                        radius = triggeredPoi.Radius,
                        priority = triggeredPoi.Priority,
                        listenCount = totalListens // Trả về số liệu thật
                    },
                    FilePath = audioPath 
                });
            }

            return Ok(new { triggered = false });
        }
    }
}