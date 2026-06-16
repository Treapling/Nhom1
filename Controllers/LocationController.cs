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

        public LocationController(AppDbContext context)
        {
            _context = context;
            _geofenceService = new GeofenceService();
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckLocation([FromQuery] double lat, [FromQuery] double lng)
        {
            // 1. Kéo toàn bộ danh sách POI từ Database SQLite
            var allPOIs = await _context.POIs.ToListAsync();

            // 2. Đưa vào thuật toán Haversine kiểm tra xem có điểm nào thỏa mãn bán kính không
            var triggeredPoi = _geofenceService.CheckTriggeredPOI(lat, lng, allPOIs);

            if (triggeredPoi != null)
            {
                return Ok(new {
                    triggered = true,
                    poi = triggeredPoi,
                    // Tạm thời fix cứng link nhạc, sau này làm CRUD Audio sẽ lấy từ DB.
                    audioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3" 
                });
            }

            return Ok(new { triggered = false });
        }
    }
}