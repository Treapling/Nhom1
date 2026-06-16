using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TourGuideProject.Models;
using TourGuideProject.Services;

namespace TourGuideProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly GeofenceService _geofenceService;

        public LocationController()
        {
            _geofenceService = new GeofenceService();
        }

        [HttpGet("check")]
        public IActionResult CheckLocation([FromQuery] double lat, [FromQuery] double lng)
        {
            // Dữ liệu giả lập (Mock POI) để test API
            var mockPOIs = new List<POI>
            {
                new POI { 
                    Id = 1, 
                    Name = "Trường Đại học Sài Gòn (SGU)", 
                    Lat = 10.7600, 
                    Lng = 106.6822, 
                    Radius = 150, // Bán kính 150 mét
                    Priority = 1 
                },
                new POI { 
                    Id = 2, 
                    Name = "Chợ Bến Thành", 
                    Lat = 10.7725, 
                    Lng = 106.6980, 
                    Radius = 200, 
                    Priority = 2 
                }
            };

            var triggeredPoi = _geofenceService.CheckTriggeredPOI(lat, lng, mockPOIs);

            if (triggeredPoi != null)
            {
                return Ok(new {
                    triggered = true,
                    poi = triggeredPoi,
                    // File audio mẫu để test tính năng phát nhạc
                    audioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3" 
                });
            }

            return Ok(new { triggered = false });
        }
    }
}