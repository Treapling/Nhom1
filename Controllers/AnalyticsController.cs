using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AnalyticsController(AppDbContext context) { _context = context; }

        // 1. Số người đang online (Tính các quét QR trong 30 phút qua)
        [HttpGet("online")]
        public async Task<IActionResult> GetOnlineUsers()
        {
            var thirtyMinsAgo = DateTime.UtcNow.AddMinutes(-30);
            var activeSessions = await _context.TrackingLogs
                .Where(t => t.Timestamp >= thirtyMinsAgo)
                .Select(t => t.SessionToken)
                .Distinct()
                .CountAsync();
            return Ok(new { onlineUsers = activeSessions });
        }

        // 2. Biểu đồ theo khung giờ
        [HttpGet("chart")]
        public async Task<IActionResult> GetChartData()
        {
            var logs = await _context.TrackingLogs.ToListAsync();
            
            // Group theo khung giờ (0h - 23h)
            var hourlyData = logs
                .GroupBy(l => l.Timestamp.ToLocalTime().Hour)
                .Select(g => new { Hour = g.Key + "h", Count = g.Count() })
                .OrderBy(g => int.Parse(g.Hour.Replace("h", "")))
                .ToList();

            return Ok(hourlyData);
        }
    }
}