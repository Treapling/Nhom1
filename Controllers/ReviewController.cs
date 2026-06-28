using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReviewController(AppDbContext context) { _context = context; }

        // 1. GỬI ĐÁNH GIÁ (Free & Premium)
        [HttpPost]
        [Authorize(Roles = "GuestFree,GuestPremium")]
        public IActionResult PostReview([FromBody] Review review)
        {
            // Ép luật: Nếu là Free, xóa Comment trắng trơn, chỉ giữ Rating
            if (User.IsInRole("GuestFree")) { review.Comment = null; }
            
            _context.Reviews.Add(review);
            _context.SaveChanges();
            return Ok(new { message = "Cảm ơn bạn đã đánh giá!" });
        }

        // 2. LẤY BÌNH LUẬN CHO KHÁCH PREMIUM XEM TRONG APP
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

        // 3. ADMIN & VENDOR QUẢN LÝ BÌNH LUẬN TOÀN HỆ THỐNG
        [HttpGet("manage")]
        [Authorize(Roles = "Admin,Vendor")]
        public async Task<IActionResult> GetAllReviews()
        {
            var query = _context.Reviews.Include(r => r.POI).AsQueryable();

            // Nếu là Vendor, lọc chỉ lấy bình luận của quán họ
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