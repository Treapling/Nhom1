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
    // Kéo toàn bộ danh sách POI từ Database SQLite KÈM THEO danh sách Audio của từng điểm
    var allPOIs = await _context.POIs
        .Include(p => p.Audios) // Dùng LINQ để nạp dữ liệu bảng Audio liên kết
        .ToListAsync();

    // Đưa vào thuật toán Haversine kiểm tra xem có điểm nào thỏa mãn bán kính không
    var triggeredPoi = _geofenceService.CheckTriggeredPOI(lat, lng, allPOIs);

    if (triggeredPoi != null)
    {
        // Lấy ra link nhạc thực tế từ Database thay vì fix cứng.
        // Mặc định lấy file đầu tiên (hoặc file tiếng Việt "vi")
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
                priority = triggeredPoi.Priority
            },
            audioUrl = audioPath // Đã đổi thành link động lấy từ database
        });
    }

    return Ok(new { triggered = false });
}
    }
}