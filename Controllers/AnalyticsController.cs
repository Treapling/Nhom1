using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AnalyticsController(AppDbContext context) { _context = context; }

        [HttpGet("kpi-summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetKPISummary()
        {
            var fiveMinsAgo = DateTime.UtcNow.AddMinutes(-5);
            
            // ĐẾM ONLINE CHUẨN XÁC: Nhờ SessionToken đã được lưu vết đầy đủ từ thiết bị
            var onlineUsers = await _context.TrackingLogs
                .Where(t => t.Timestamp >= fiveMinsAgo && t.SessionToken != null)
                .Select(t => t.SessionToken)
                .Distinct()
                .CountAsync();

            var totalActivePOIs = await _context.POIs.CountAsync(p => p.ApprovalStatus == 1);
            var pendingPOIs = await _context.POIs.CountAsync(p => p.ApprovalStatus == 0);
            var totalReviews = await _context.Reviews.CountAsync(); 
            var totalScans = await _context.TrackingLogs.CountAsync(); 

            // ===================================================================
            // THUẬT TOÁN ĐIỀU CHỈNH BAYESIAN AVERAGE NÂNG CAO (ỨNG DỤNG THỰC TẾ)
            // ===================================================================
            var allReviews = await _context.Reviews.ToListAsync();
            var activePois = await _context.POIs.Where(p => p.ApprovalStatus == 1).ToListAsync();

            // 1. C: Điểm đánh giá trung bình của toàn bộ hệ thống
            double globalAvgRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 3.0;

            // 2. m: Số lượng đánh giá trung bình trên một địa điểm (Ngưỡng Bayes)
            double minReviewsThreshold = activePois.Any() ? (double)allReviews.Count / activePois.Count : 1.0;
            if (minReviewsThreshold < 1.0) minReviewsThreshold = 1.0;

            var rawPoiStats = activePois.Select(p => {
                int listens = _context.TrackingLogs.Count(t => t.POI_Id == p.Id);
                var pReviews = allReviews.Where(r => r.POI_Id == p.Id).ToList();
                
                int v = pReviews.Count; // Số đánh giá của quán này
                double R = v > 0 ? pReviews.Average(r => r.Rating) : globalAvgRating; // Điểm TB của quán này

                // Công thức cân bằng trọng số Bayes chống gian lận điểm số
                double bayesianRating = ((v / (v + minReviewsThreshold)) * R) + ((minReviewsThreshold / (v + minReviewsThreshold)) * globalAvgRating);
                
                // Kết hợp mượt độ tăng trưởng lượt nghe bằng hàm Logarit tự nhiên
                double finalRankScore = (bayesianRating * 0.6) + (Math.Log(listens + 1) * 0.4);

                return new {
                    p.Name,
                    Scans = listens,
                    Rating = Math.Round(R, 1),
                    Reviews = v,
                    Score = finalRankScore
                };
            }).ToList();

            var hottestPOI = rawPoiStats.OrderByDescending(x => x.Score).Select(x => new {
                name = x.Name,
                scans = x.Scans,
                rating = x.Rating,
                reviews = x.Reviews,
                score = Math.Round(x.Score, 2)
            }).FirstOrDefault();

            return Ok(new { onlineUsers, totalActivePOIs, pendingPOIs, totalReviews, totalScans, hottestPOI });
        }

        [HttpGet("chart")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetChartData([FromQuery] string type = "hour")
        {
            var logs = await _context.TrackingLogs.ToListAsync();
            var now = DateTime.UtcNow.ToLocalTime();
            
            if (type == "day")
            {
                int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                var baseLabels = Enumerable.Range(1, daysInMonth).Select(d => "Ngày " + d).ToList();
                
                var grouped = logs.Where(l => l.Timestamp.ToLocalTime().Month == now.Month && l.Timestamp.ToLocalTime().Year == now.Year)
                    .GroupBy(l => l.Timestamp.ToLocalTime().Day)
                    .ToDictionary(g => "Ngày " + g.Key, g => g.Count());

                var result = baseLabels.Select(lbl => new { Label = lbl, Count = grouped.ContainsKey(lbl) ? grouped[lbl] : 0 });
                return Ok(result);
            }
            else if (type == "month")
            {
                var baseLabels = Enumerable.Range(1, 12).Select(m => "Tháng " + m).ToList();

                var grouped = logs.Where(l => l.Timestamp.ToLocalTime().Year == now.Year)
                    .GroupBy(l => l.Timestamp.ToLocalTime().Month)
                    .ToDictionary(g => "Tháng " + g.Key, g => g.Count());

                var result = baseLabels.Select(lbl => new { Label = lbl, Count = grouped.ContainsKey(lbl) ? grouped[lbl] : 0 });
                return Ok(result);
            }
            else 
            {
                var baseLabels = Enumerable.Range(0, 24).Select(h => h + "h").ToList();

                var grouped = logs.Where(l => l.Timestamp.ToLocalTime().Date == now.Date)
                    .GroupBy(l => l.Timestamp.ToLocalTime().Hour)
                    .ToDictionary(g => g.Key + "h", g => g.Count());

                var result = baseLabels.Select(lbl => new { Label = lbl, Count = grouped.ContainsKey(lbl) ? grouped[lbl] : 0 });
                return Ok(result);
            }
        }

        [HttpGet("vendor-kpi")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetVendorKPI()
        {
            var vendorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (vendorIdClaim == null) return Unauthorized();
            int vendorId = int.Parse(vendorIdClaim);

            var totalPois = await _context.POIs.CountAsync(p => p.UserId == vendorId && p.ApprovalStatus == 1);
            var pendingPois = await _context.POIs.CountAsync(p => p.UserId == vendorId && p.ApprovalStatus == 0);
            var totalReviews = await _context.Reviews.CountAsync(r => r.POI.UserId == vendorId);
            var totalScans = await _context.TrackingLogs.CountAsync(t => t.POI.UserId == vendorId);

            var allReviews = await _context.Reviews.ToListAsync();
            var globalAvgRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 3.0;
            var allActivePois = await _context.POIs.Where(p => p.ApprovalStatus == 1).ToListAsync();
            double minReviewsThreshold = allActivePois.Any() ? (double)allReviews.Count / allActivePois.Count : 1.0;
            if (minReviewsThreshold < 1.0) minReviewsThreshold = 1.0;

            var myPois = allActivePois.Where(p => p.UserId == vendorId).ToList();

            var hottestPOI = myPois.Select(p => {
                int listens = _context.TrackingLogs.Count(t => t.POI_Id == p.Id);
                var pReviews = allReviews.Where(r => r.POI_Id == p.Id).ToList();
                
                int v = pReviews.Count;
                double R = v > 0 ? pReviews.Average(r => r.Rating) : globalAvgRating;

                double bayesianRating = ((v / (v + minReviewsThreshold)) * R) + ((minReviewsThreshold / (v + minReviewsThreshold)) * globalAvgRating);
                double finalRankScore = (bayesianRating * 0.6) + (Math.Log(listens + 1) * 0.4);

                return new {
                    name = p.Name,
                    scans = listens,
                    rating = Math.Round(R, 1),
                    reviews = v,
                    score = finalRankScore
                };
            }).OrderByDescending(x => x.score).FirstOrDefault();

            return Ok(new { totalPois, pendingPois, totalReviews, totalScans, hottestPOI });
        }

        [HttpPost("logout-guest")]
        [Authorize]
        public async Task<IActionResult> LogoutGuest()
        {
            var sessionToken = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (sessionToken != null)
            {
            // Xóa sạch lịch sử 5 phút qua của riêng User này để giải phóng chỗ cho phiên mới
                var userLogs = _context.TrackingLogs.Where(t => t.SessionToken == sessionToken);
                _context.TrackingLogs.RemoveRange(userLogs);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}