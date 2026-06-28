using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Nhom1.Models; 
using Nhom1.Data;
using System.IO;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải đăng nhập mới được thao tác với file
    public class AudioController : ControllerBase
    {
        private readonly AppDbContext _context; 
        private readonly IWebHostEnvironment _env; // Cần dùng để lấy đường dẫn thư mục vật lý

        public AudioController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. API Tải file âm thanh lên (Dùng Form Data thay vì JSON)
        [HttpPost]
        public async Task<IActionResult> UploadAudio([FromForm] int poiId, [FromForm] string language, [FromForm] bool isPremium, IFormFile file)
{
            // Kiểm tra tính hợp lệ
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file âm thanh (.mp3)" });
                
            var poi = await _context.POIs.FindAsync(poiId);
            if (poi == null)
                return BadRequest(new { message = "Địa điểm không tồn tại." });

            // RLS: Nếu người upload là Vendor, kiểm tra xem họ có sở hữu POI này không
            if (User.IsInRole("Vendor"))
            {
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                if (poi.UserId != vendorId)
                    return Forbid();
            }

            // Tạo thư mục nếu chưa có
            var uploadPath = Path.Combine(_env.WebRootPath, "audios");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Đổi tên file để tránh trùng lặp: {POI_ID}_{Language}_{Timestamp}.mp3
            var fileName = $"{poiId}_{language}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Lưu file vật lý xuống ổ cứng Server
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Lưu đường dẫn vào CSDL
            var audioRecord = new Audio
            {
                POI_Id = poiId,
        Language = language,
        FilePath = $"/audios/{fileName}",
        IsPremium = isPremium // Lưu cờ phân loại âm thanh
            };

            _context.Audios.Add(audioRecord);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tải file âm thanh thành công!", audio = audioRecord });
        }

        // 2. Lấy danh sách Audio của 1 POI
        [HttpGet("poi/{poiId}")]
        [AllowAnonymous] // Khách không đăng nhập cũng gọi được
        public async Task<IActionResult> GetAudiosByPoi(int poiId)
        {
            var audios = await _context.Audios.Where(a => a.POI_Id == poiId).ToListAsync();
            return Ok(audios);
        }

        // 3. Xóa File âm thanh
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null)
                return NotFound(new { message = "Không tìm thấy file âm thanh!" });

            // RLS
            if (User.IsInRole("Vendor"))
            {
                var poi = await _context.POIs.FindAsync(audio.POI_Id);
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                if (poi.UserId != vendorId) return Forbid();
            }

            // Xóa file vật lý trên Server
            var physicalPath = Path.Combine(_env.WebRootPath, audio.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            // Xóa dòng trong CSDL
            _context.Audios.Remove(audio);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa file âm thanh thành công." }); 
        }
    }
}