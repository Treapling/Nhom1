using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Nhom1.Models; 
using Nhom1.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class AudioController : ControllerBase
    {
        private readonly AppDbContext _context; 
        private readonly IWebHostEnvironment _env; 

        public AudioController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> UploadAudio([FromForm] int poiId, [FromForm] string language, [FromForm] bool isPremium, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file âm thanh (.mp3)" });
                
            var poi = await _context.POIs.FindAsync(poiId);
            if (poi == null)
                return BadRequest(new { message = "Địa điểm không tồn tại." });

            if (User.IsInRole("Vendor"))
            {
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                if (poi.UserId != vendorId)
                    return Forbid();
            }

            // KIỂM TRA TRÙNG LẶP (DUPLICATE VALIDATION)
            bool isDuplicate = await _context.Audios.AnyAsync(a => a.POI_Id == poiId && a.Language == language && a.IsPremium == isPremium);
            if (isDuplicate)
            {
                return BadRequest(new { message = "Ngôn ngữ + loại gói này đã có file audio!!" });
            }

            var uploadPath = Path.Combine(_env.WebRootPath, "audios");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fileName = $"{poiId}_{language}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var audioRecord = new Audio
            {
                POI_Id = poiId,
                Language = language,
                FilePath = $"/audios/{fileName}",
                IsPremium = isPremium 
            };

            _context.Audios.Add(audioRecord);
            await _context.SaveChangesAsync();

            // Đổi lời nhắn thành công ở Backend (phòng hờ)
            return Ok(new { message = "Đã tải file lên thành công!", audio = audioRecord });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> UpdateAudio(int id, [FromBody] Audio updateData)
        {
            var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null) return NotFound();

            var poi = await _context.POIs.FindAsync(audio.POI_Id);
            if (poi.UserId != vendorId) return Forbid();

            bool isDuplicate = await _context.Audios.AnyAsync(a => a.POI_Id == audio.POI_Id && a.Id != id && a.Language == updateData.Language && a.IsPremium == updateData.IsPremium);
            if (isDuplicate) return BadRequest(new { message = "Ngôn ngữ + loại gói này đã có file audio!!" });

            audio.Language = updateData.Language;
            audio.IsPremium = updateData.IsPremium;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật thông tin Audio." });
        }

        [HttpGet("poi/{poiId}")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetAudiosByPoi(int poiId)
        {
            var audios = await _context.Audios.Where(a => a.POI_Id == poiId).ToListAsync();
            return Ok(audios);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null)
                return NotFound(new { message = "Không tìm thấy file âm thanh!" });

            if (User.IsInRole("Vendor"))
            {
                var poi = await _context.POIs.FindAsync(audio.POI_Id);
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                if (poi.UserId != vendorId) return Forbid();
            }

            var physicalPath = Path.Combine(_env.WebRootPath, audio.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.Audios.Remove(audio);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa file âm thanh thành công." }); 
        }
    }
}