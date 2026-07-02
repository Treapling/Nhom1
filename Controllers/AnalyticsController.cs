using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nhom1.Controllers
{
    /// <summary>
    /// [CONTROLLER] Thống kê & Phân tích - Cung cấp dữ liệu dashboard cho Admin và Vendor
    /// Bao gồm: KPI tổng quan, dữ liệu biểu đồ, xếp hạng POI theo thuật toán Bayesian Average
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AnalyticsController(AppDbContext context) { _context = context; }

        /// <summary>
        /// [GET /api/analytics/kpi-summary] - [Admin] Dashboard tổng quan
        /// Trả về:
        /// - onlineUsers: số user online trong 5 phút gần nhất (dựa trên SessionToken duy nhất)
        /// - totalActivePOIs: tổng số POI đang hoạt động (ApprovalStatus = 1)
        /// - pendingPOIs: số POI chờ duyệt (ApprovalStatus = 0)
        /// - totalReviews: tổng số đánh giá
        /// - totalScans: tổng lượt quét QR / GPS trigger
        /// - hottestPOI: địa điểm "hot nhất" dựa trên thuật toán Bayesian Average Ranking
        /// 
        /// *THUẬT TOÁN BAYESIAN AVERAGE:
        /// - C: điểm trung bình toàn hệ thống
        /// - m: ngưỡng tối thiểu (số review trung bình 1 POI)
        /// - R: điểm trung bình của từng POI
        /// - v: số lượng review của POI đó
        /// => BayesianRating = (v/(v+m))*R + (m/(v+m))*C
        /// => FinalScore = BayesianRating*0.6 + Log(lượt nghe+1)*0.4
        /// Mục đích: chống gian lận điểm, POI mới ít review không bị điểm quá cao/thấp
        /// </summary>
        [HttpGet("kpi-summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetKPISummary()
        {
            var thirtySecsAgo = DateTime.UtcNow.AddSeconds(-30);
            
            // ĐẾM ONLINE CHUẨN XÁC: Nhờ SessionToken đã được lưu vết đầy đủ từ thiết bị
            var onlineUsers = await _context.TrackingLogs
                .Where(t => t.Timestamp >= thirtySecsAgo && t.SessionToken != null)
                .Select(t => t.SessionToken)
                .Distinct()
                .CountAsync();

            var totalActivePOIs = await _context.POIs.CountAsync(p => p.ApprovalStatus == 1);
            var pendingPOIs = await _context.POIs.CountAsync(p => p.ApprovalStatus == 0);
            var totalReviews = await _context.Reviews.CountAsync();
            var totalScans = await _context.TrackingLogs.CountAsync();

            // ================================================================
            // THUẬT TOÁN BAYESIAN AVERAGE - XẾP HẠNG POI
            // ================================================================
            var allReviews = await _context.Reviews.ToListAsync();
            var activePois = await _context.POIs.Where(p => p.ApprovalStatus == 1).ToListAsync();

            // C: Điểm đánh giá trung bình toàn hệ thống
            double globalAvgRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 3.0;

            // m: Ngưỡng Bayes = số review trung bình trên 1 địa điểm
            double minReviewsThreshold = activePois.Any() ? (double)allReviews.Count / activePois.Count : 1.0;
            if (minReviewsThreshold < 1.0) minReviewsThreshold = 1.0;

            var rawPoiStats = activePois.Select(p => {
                int listens = _context.TrackingLogs.Count(t => t.POI_Id == p.Id);
                var pReviews = allReviews.Where(r => r.POI_Id == p.Id).ToList();

                int v = pReviews.Count;        // Số review của quán này
                double R = v > 0 ? pReviews.Average(r => r.Rating) : globalAvgRating; // Điểm TB của quán

                // Công thức Bayesian: cân bằng giữa điểm riêng và điểm toàn hệ thống
                double bayesianRating = ((v / (v + minReviewsThreshold)) * R) + ((minReviewsThreshold / (v + minReviewsThreshold)) * globalAvgRating);

                // Kết hợp với lượt nghe (dùng Log để giảm độ chênh lệch)
                double finalRankScore = (bayesianRating * 0.6) + (Math.Log(listens + 1) * 0.4);

                return new {
                    p.Name,
                    Scans = listens,
                    Rating = Math.Round(R, 1),
                    Reviews = v,
                    Score = finalRankScore
                };
            }).ToList();

            // POI có điểm cao nhất
            var hottestPOI = rawPoiStats.OrderByDescending(x => x.Score).Select(x => new {
                name = x.Name,
                scans = x.Scans,
                rating = x.Rating,
                reviews = x.Reviews,
                score = Math.Round(x.Score, 2)
            }).FirstOrDefault();

            return Ok(new { onlineUsers, totalActivePOIs, pendingPOIs, totalReviews, totalScans, hottestPOI });
        }

        /// <summary>
        /// [GET /api/analytics/chart?type=] - [Admin] Dữ liệu biểu đồ thống kê lượt tương tác
        /// type = "hour": 24h trong ngày hôm nay (0h -> 23h)
        /// type = "day": các ngày trong tháng hiện tại (Ngày 1 -> Ngày 31)
        /// type = "month" (mặc định): 12 tháng trong năm hiện tại (Tháng 1 -> Tháng 12)
        /// </summary>
        [HttpGet("chart")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetChartData([FromQuery] string type = "hour")
        {
            var logs = await _context.TrackingLogs.ToListAsync();
            var now = DateTime.UtcNow.ToLocalTime();

            if (type == "day")
            {
                // Biểu đồ theo ngày trong tháng
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
                // Biểu đồ theo tháng trong năm
                var baseLabels = Enumerable.Range(1, 12).Select(m => "Tháng " + m).ToList();

                var grouped = logs.Where(l => l.Timestamp.ToLocalTime().Year == now.Year)
                    .GroupBy(l => l.Timestamp.ToLocalTime().Month)
                    .ToDictionary(g => "Tháng " + g.Key, g => g.Count());

                var result = baseLabels.Select(lbl => new { Label = lbl, Count = grouped.ContainsKey(lbl) ? grouped[lbl] : 0 });
                return Ok(result);
            }
            else
            {
                // Biểu đồ theo giờ trong ngày (mặc định)
                var baseLabels = Enumerable.Range(0, 24).Select(h => h + "h").ToList();

                var grouped = logs.Where(l => l.Timestamp.ToLocalTime().Date == now.Date)
                    .GroupBy(l => l.Timestamp.ToLocalTime().Hour)
                    .ToDictionary(g => g.Key + "h", g => g.Count());

                var result = baseLabels.Select(lbl => new { Label = lbl, Count = grouped.ContainsKey(lbl) ? grouped[lbl] : 0 });
                return Ok(result);
            }
        }

        /// <summary>
        /// [GET /api/analytics/vendor-kpi] - [Vendor] Thống kê riêng cho chủ quán
        /// Trả về: tổng POI đã duyệt, POI chờ duyệt, tổng review, tổng lượt quét, POI hot nhất
        /// (Lọc theo UserId = VendorId)
        /// </summary>
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

            // Tính Bayesian Average cho các POI của Vendor (giống thuật toán bên Admin)
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

        /// <summary>
        /// [POST /api/analytics/logout-guest] - [Auth] Xóa lịch sử tracking khi guest logout
        /// Xóa tất cả TrackingLogs có SessionToken trùng với token hiện tại
        /// (Giải phóng dữ liệu phiên, chuẩn bị cho phiên mới)
        /// </summary>
        [HttpPost("logout-guest")]
        [Authorize]
        public async Task<IActionResult> LogoutGuest()
        {
            var sessionToken = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (sessionToken != null)
            {
                var userLogs = _context.TrackingLogs.Where(t => t.SessionToken == sessionToken);
                _context.TrackingLogs.RemoveRange(userLogs);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}