using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        // Khách vãng lai (có token 24h) được gửi đánh giá
        [HttpPost]
        [Authorize(Roles = "Guest")]
        public IActionResult PostReview([FromBody] Review review)
        {
            _context.Reviews.Add(review);
            _context.SaveChanges();
            return Ok(new { message = "Cảm ơn bạn đã đánh giá!" });
        }
    }
}