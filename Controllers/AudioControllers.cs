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
    /// <summary>
    /// [CONTROLLER] Quản lý Audio - Upload, cập nhật, xóa file âm thanh cho từng địa điểm (POI)
    /// File audio được lưu trong thư mục wwwroot/audios/
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Mặc định tất cả các action đều yêu cầu xác thực
    public class AudioController : ControllerBase
    {
        private readonly AppDbContext _context; 
        private readonly IWebHostEnvironment _env; // Môi trường web (lấy đường dẫn wwwroot)

        public AudioController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// [POST /api/audio] - [Auth] Upload file âm thanh mới cho một địa điểm
        /// Tham số: poiId (ID địa điểm), language (ngôn ngữ), isPremium (true/false), file (file .mp3)
        /// Kiểm tra: POI tồn tại, quyền sở hữu (Vendor chỉ upload cho POI của mình), trùng lặp ngôn ngữ + gói
        /// File được đặt tên theo format: {poiId}_{language}_{timestamp}.mp3
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadAudio([FromForm] int poiId, [FromForm] string language, [FromForm] bool isPremium, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file âm thanh (.mp3)" });

            var poi = await _context.POIs.FindAsync(poiId);
            if (poi == null)
                return BadRequest(new { message = "Địa điểm không tồn tại." });

            // Kiểm tra quyền: Vendor chỉ được upload cho POI của mình
            if (User.IsInRole("Vendor"))
            {
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                if (poi.UserId != vendorId)
                    return Forbid();
            }

            // Kiểm tra trùng lặp: 1 POI chỉ có 1 file audio cho 1 ngôn ngữ + 1 loại gói
            bool isDuplicate = await _context.Audios.AnyAsync(a => a.POI_Id == poiId && a.Language == language && a.IsPremium == isPremium);
            if (isDuplicate)
            {
                return BadRequest(new { message = "Ngôn ngữ + loại gói này đã có file audio!!" });
            }

            // Tạo thư mục audios nếu chưa tồn tại
            var uploadPath = Path.Combine(_env.WebRootPath, "audios");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Đặt tên file: {poiId}_{ngôn ngữ}_{timestamp}.mp3
            var fileName = $"{poiId}_{language}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Ghi file vào ổ đĩa
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Lưu thông tin audio vào database
            var audioRecord = new Audio
            {
                POI_Id = poiId,
                Language = language,
                FilePath = $"/audios/{fileName}",
                IsPremium = isPremium 
            };

            _context.Audios.Add(audioRecord);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã tải file lên thành công!", audio = audioRecord });
        }

        /// <summary>
        /// [PUT /api/audio/{id}] - [Vendor] Cập nhật thông tin audio (ngôn ngữ, isPremium)
        /// Kiểm tra quyền sở hữu thông qua POI
        /// Kiểm tra trùng lặp trước khi cập nhật
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> UpdateAudio(int id, [FromBody] Audio updateData)
        {
            var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null) return NotFound();

            var poi = await _context.POIs.FindAsync(audio.POI_Id);
            if (poi.UserId != vendorId) return Forbid();

            // Kiểm tra trùng lặp (loại trừ chính nó)
            bool isDuplicate = await _context.Audios.AnyAsync(a => a.POI_Id == audio.POI_Id && a.Id != id && a.Language == updateData.Language && a.IsPremium == updateData.IsPremium);
            if (isDuplicate) return BadRequest(new { message = "Ngôn ngữ + loại gói này đã có file audio!!" });

            audio.Language = updateData.Language;
            audio.IsPremium = updateData.IsPremium;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật thông tin Audio." });
        }

        /// <summary>
        /// [GET /api/audio/poi/{poiId}] - [Anonymous] Lấy danh sách audio của một địa điểm
        /// Không yêu cầu xác thực (AllowAnonymous) để phục vụ client frontend
        /// </summary>
        [HttpGet("poi/{poiId}")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetAudiosByPoi(int poiId)
        {
            var audios = await _context.Audios.Where(a => a.POI_Id == poiId).ToListAsync();
            return Ok(audios);
        }

        /// <summary>
        /// [DELETE /api/audio/{id}] - [Auth] Xóa file âm thanh
        /// Xóa cả file vật lý trên ổ đĩa và bản ghi trong database
        /// Kiểm tra quyền: Vendor chỉ xóa được audio của POI mình
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null)
                return NotFound(new { message = "Không tìm thấy file âm thanh!" });

            // Kiểm tra quyền sở hữu nếu là Vendor
            if (User.IsInRole("Vendor"))
            {
                var poi = await _context.POIs.FindAsync(audio.POI_Id);
                var vendorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                if (poi.UserId != vendorId) return Forbid();
            }

            // Xóa file vật lý
            var physicalPath = Path.Combine(_env.WebRootPath, audio.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            // Xóa bản ghi trong database
            _context.Audios.Remove(audio);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa file âm thanh thành công." }); 
        }
    }
}