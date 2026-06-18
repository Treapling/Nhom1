using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom1.Models; 
using Nhom1.Data;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        private readonly AppDbContext _context; 

        public AudioController(AppDbContext context)
        {
            _context = context;
        }

        // POST
        [HttpPost]
        public async Task<IActionResult> CreateAudio([FromBody] Audio audio)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // CHECK
            var poiExists = await _context.POIs.AnyAsync(p => p.Id == audio.POI_Id);
            if (!poiExists)
            {
                return BadRequest("Mã địa điểm (POI_Id) không tồn tại trong hệ thống!");
            }

            _context.Audios.Add(audio);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAudioById), new { id = audio.Id }, audio);
        }

        // 2. GET FILE
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAudioById(int id)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null)
            {
                return NotFound("Không tìm thấy file âm thanh yêu cầu!");
            }

            return Ok(audio);
        }

        // 3. DELETE FILE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null)
            {
                return NotFound("Không tìm thấy file âm thanh cần xóa!");
            }

            _context.Audios.Remove(audio);
            await _context.SaveChangesAsync();

            return NoContent(); 
        }
    }
}