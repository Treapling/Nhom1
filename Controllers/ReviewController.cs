using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;

namespace Nhom1.Controllers
{
    /// <summary>
    /// [CONTROLLER] Đánh giá & Bình luận (Review) - Xử lý đánh giá sao và bình luận cho địa điểm
    /// Phân quyền: GuestFree chỉ được đánh giá sao (không comment), GuestPremium có thể xem bình luận
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReviewController(AppDbContext context) { _context = context; }

        /// <summary>
        /// [POST /api/review] - [GuestFree/GuestPremium] Gửi đánh giá cho địa điểm
        /// - GuestFree: chỉ được gửi Rating, Comment bị xóa (set null) 
        /// - GuestPremium: được gửi cả Rating và Comment
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "GuestFree,GuestPremium")]
        public IActionResult PostReview([FromBody] Review review)
        {
            // Nếu là GuestFree, xóa comment để chặn việc gửi bình luận
            if (User.IsInRole("GuestFree")) { review.Comment = null; }

            _context.Reviews.Add(review);
            _context.SaveChanges();
            return Ok(new { message = "Cảm ơn bạn đã đánh giá!" });
        }

        /// <summary>
        /// [GET /api/review/poi/{poiId}] - [GuestPremium] Lấy bình luận của địa điểm
        /// Chỉ GuestPremium mới được xem nội dung bình luận
        /// Trả về: rating, comment, thời gian tạo (sắp xếp mới nhất trước)
        /// </summary>
        [HttpGet("poi/{poiId}")]
        [Authorize(Roles = "GuestPremium")]
        public async Task<IActionResult> GetReviewsForPremium(int poiId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.POI_Id == poiId && !string.IsNullOrEmpty(r.Comment))
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Rating, r.Comment, r.CreatedAt })
                .ToListAsync();
            return Ok(reviews);
        }

        /// <summary>
        /// [GET /api/review/manage] - [Admin/Vendor] Quản lý tất cả đánh giá
        /// - Admin: xem tất cả đánh giá trong hệ thống
        /// - Vendor: chỉ xem đánh giá của các POI thuộc quyền quản lý của mình
        /// Trả về: id, rating, comment, thời gian, tên POI
        /// </summary>
        [HttpGet("manage")]
        [Authorize(Roles = "Admin,Vendor")]
        public async Task<IActionResult> GetAllReviews()
        {
            var query = _context.Reviews.Include(r => r.POI).AsQueryable();

            // Nếu là Vendor, lọc chỉ lấy review của quán họ
            if (User.IsInRole("Vendor"))
            {
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                query = query.Where(r => r.POI.UserId == vendorId);
            }

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Id, r.Rating, r.Comment, r.CreatedAt, PoiName = r.POI.Name })
                .ToListAsync();

            return Ok(reviews);
        }
    }
}