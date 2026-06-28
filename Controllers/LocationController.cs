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
            // TỐI ƯU HÓA: Chỉ lấy các điểm đã duyệt và KHÔNG Include Audios để tránh quá tải RAM
            var allPOIs = await _context.POIs
                .Where(p => p.ApprovalStatus == 1)
                .ToListAsync();

            var triggeredPoi = _geofenceService.CheckTriggeredPOI(lat, lng, allPOIs);

            if (triggeredPoi != null)
            {
                // Frontend sẽ tự động gọi GetPOI(id) để lấy chi tiết và ghi log.
                // Trả về ID cực nhẹ, không nhồi nhét data dư thừa
                return Ok(new {
                    triggered = true,
                    poi = new { id = triggeredPoi.Id } 
                });
            }

            return Ok(new { triggered = false });
        }
    }
}