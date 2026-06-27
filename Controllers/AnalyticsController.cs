using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;
using System.Collections.Concurrent;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        // Biến static để lưu trạng thái các session đang online (kèm thời gian ping cuối)
        private static readonly ConcurrentDictionary<string, DateTime> _activeUsers = new();

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        public class PingRequest
        {
            public string SessionId { get; set; }
        }

        // 1. API Nhận tín hiệu Ping từ hành khách (Real-time active users)
        [HttpPost("ping")]
        public async Task<IActionResult> Ping([FromBody] PingRequest request)
        {
            if (string.IsNullOrEmpty(request.SessionId))
                return BadRequest();

            // Cập nhật thời gian truy cập cuối cùng cho session này vào RAM
            _activeUsers[request.SessionId] = DateTime.UtcNow;

            // Xóa các session đã không ping trong 3 phút (coi như đã tắt web)
            var threshold = DateTime.UtcNow.AddMinutes(-3);
            var inactiveSessions = _activeUsers.Where(kvp => kvp.Value < threshold).Select(kvp => kvp.Key).ToList();
            foreach (var inactive in inactiveSessions)
            {
                _activeUsers.TryRemove(inactive, out _);
            }

            // Ghi vào DB để phục vụ thống kê (Chỉ ghi 1 lần mỗi session mỗi ngày để tránh rác DB)
            var today = DateTime.UtcNow.Date;
            var hasVisitedToday = await _context.VisitorLogs
                .AnyAsync(v => v.SessionId == request.SessionId && v.Timestamp >= today);

            if (!hasVisitedToday)
            {
                _context.VisitorLogs.Add(new VisitorLog
                {
                    SessionId = request.SessionId,
                    Timestamp = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Ping success" });
        }

        // 2. API Trả về dữ liệu Thống kê cho Admin Dashboard
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            // A. Dọn dẹp lại session ảo trước khi đếm
            var threshold = DateTime.UtcNow.AddMinutes(-3);
            var inactiveSessions = _activeUsers.Where(kvp => kvp.Value < threshold).Select(kvp => kvp.Key).ToList();
            foreach (var inactive in inactiveSessions)
            {
                _activeUsers.TryRemove(inactive, out _);
            }
            int activeCount = _activeUsers.Count;

            // B. Thống kê theo Tháng (Năm nay)
            var currentYear = DateTime.UtcNow.Year;
            var monthStats = await _context.VisitorLogs
                .Where(v => v.Timestamp.Year == currentYear)
                .GroupBy(v => v.Timestamp.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var visitsByMonth = new int[12];
            foreach (var stat in monthStats)
            {
                visitsByMonth[stat.Month - 1] = stat.Count; // Index 0 = Tháng 1
            }

            // C. Thống kê theo Khung Giờ trong ngày (Dành cho hôm nay)
            var today = DateTime.UtcNow.Date;
            var hourStats = await _context.VisitorLogs
                .Where(v => v.Timestamp >= today)
                .GroupBy(v => v.Timestamp.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToListAsync();

            var visitsByHour = new int[24];
            foreach (var stat in hourStats)
            {
                // Convert to UTC+7 (Vietnam Time) for display roughly, or just keep UTC.
                // It's better to add 7 hours modulo 24 to align with local time.
                int localHour = (stat.Hour + 7) % 24;
                visitsByHour[localHour] += stat.Count;
            }

            return Ok(new
            {
                ActiveUsers = activeCount,
                VisitsByMonth = visitsByMonth,
                VisitsByHour = visitsByHour
            });
        }
    }
}
