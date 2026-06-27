using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Models;
using System.Text.Json;

namespace Nhom1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class POIController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Tiêm DbContext vào để tương tác với SQLite
        public POIController(AppDbContext context)
        {
            _context = context;
        }

        // API 1: Lấy danh sách toàn bộ POI kèm Audio đã được chuẩn hóa cấu trúc JSON
        [HttpGet]
        public async Task<IActionResult> GetPOIs()
        {
            var pois = await _context.POIs
                .Where(p => p.Status == "Approved" || p.Status == null || p.Status == "PendingEditApproval" || p.Status == "PendingDeleteApproval") // null check để an toàn cho data cũ
                .Include(p => p.Audios)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    lat = p.Lat,
                    lng = p.Lng,
                    radius = p.Radius,
                    priority = p.Priority,
                    // Ép cấu hình chữ thường (camelCase) để khớp 100% với file admin.html phía Frontend
                    audios = p.Audios.Select(a => new {
                        id = a.Id,
                        filePath = a.FilePath,
                        language = a.Language,
                        poI_Id = a.POI_Id
                    }).ToList()
                })
                .ToListAsync();

            return Ok(pois);
        }

        // API 2: Thêm mới một POI (Dùng cho trang CMS Admin)
        [HttpPost]
        public async Task<ActionResult<POI>> PostPOI(POI pOI)
        {
            // Admin thêm thì luôn Approved, Vendor thêm thì truyền Status từ frontend
            if (string.IsNullOrEmpty(pOI.Status))
                pOI.Status = "Approved";

            _context.POIs.Add(pOI);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPOI), new { id = pOI.Id }, pOI);
        }

        // API 3: Lấy chi tiết 1 POI
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPOI(int id)
        {
            var pOI = await _context.POIs
                .Include(p => p.Audios)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    lat = p.Lat,
                    lng = p.Lng,
                    radius = p.Radius,
                    priority = p.Priority,
                    audios = p.Audios.Select(a => new {
                        id = a.Id,
                        filePath = a.FilePath,
                        language = a.Language,
                        poI_Id = a.POI_Id
                    }).ToList()
                })
                .FirstOrDefaultAsync(p => p.id == id);

            if (pOI == null)
            {
                return NotFound();
            }

            return Ok(pOI);
        }

        // API 4: Xóa một POI
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePOI(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);
            if (pOI == null)
            {
                return NotFound();
            }

            _context.POIs.Remove(pOI);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // --- CÁC API MỚI DÀNH CHO VENDOR VÀ ADMIN DUYỆT ---

        // Lấy danh sách sạp hàng của một Vendor cụ thể
        [HttpGet("vendor/{vendorId}")]
        public async Task<IActionResult> GetVendorPOIs(int vendorId)
        {
            var pois = await _context.POIs.Where(p => p.VendorId == vendorId).ToListAsync();
            return Ok(pois);
        }

        // Vendor xác nhận thanh toán -> Đổi trạng thái từ PendingPayment sang PendingApproval
        [HttpPost("vendor/checkout")]
        public async Task<IActionResult> VendorCheckout([FromBody] CheckoutRequest request)
        {
            var pois = await _context.POIs
                .Where(p => request.PoiIds.Contains(p.Id) && p.VendorId == request.VendorId && p.Status == "PendingPayment")
                .ToListAsync();

            foreach (var p in pois)
            {
                p.Status = "PendingApproval";
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xác nhận thanh toán chờ duyệt" });
        }

        // Lấy danh sách các sạp chờ duyệt (Dành cho Admin)
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPOIs()
        {
            var pois = await _context.POIs.Where(p => p.Status == "PendingApproval" || p.Status == "PendingEditApproval" || p.Status == "PendingDeleteApproval").ToListAsync();
            return Ok(pois);
        }

        // Admin duyệt sạp hàng
        [HttpPut("approve/{id}")]
        public async Task<IActionResult> ApprovePOI(int id)
        {
            var p = await _context.POIs.FindAsync(id);
            if (p == null) return NotFound();

            if (p.Status == "PendingDeleteApproval")
            {
                _context.POIs.Remove(p);
            }
            else
            {
                if (p.Status == "PendingEditApproval" && !string.IsNullOrEmpty(p.PendingChanges))
                {
                    var changes = JsonSerializer.Deserialize<POI>(p.PendingChanges);
                    if (changes != null)
                    {
                        p.Name = changes.Name;
                        p.Description = changes.Description;
                        p.Lat = changes.Lat;
                        p.Lng = changes.Lng;
                        p.Radius = changes.Radius;
                    }
                }
                p.PendingChanges = null;
                p.Status = "Approved";
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã duyệt" });
        }

        // Admin từ chối sạp hàng
        [HttpPut("reject/{id}")]
        public async Task<IActionResult> RejectPOI(int id)
        {
            var p = await _context.POIs.FindAsync(id);
            if (p == null) return NotFound();

            if (p.Status == "PendingEditApproval" || p.Status == "PendingDeleteApproval")
            {
                p.Status = "Approved";
                p.PendingChanges = null;
            }
            else
            {
                p.Status = "Rejected";
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối" });
        }

        // Yêu cầu sửa sạp hàng từ Vendor
        [HttpPut("vendor/request-edit/{id}")]
        public async Task<IActionResult> RequestEditPOI(int id, [FromBody] POI updatedPoi)
        {
            var p = await _context.POIs.FindAsync(id);
            if (p == null) return NotFound();

            var changes = new
            {
                Name = updatedPoi.Name,
                Description = updatedPoi.Description,
                Lat = updatedPoi.Lat,
                Lng = updatedPoi.Lng,
                Radius = updatedPoi.Radius
            };
            p.PendingChanges = JsonSerializer.Serialize(changes);
            p.Status = "PendingEditApproval";
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã gửi yêu cầu sửa, chờ duyệt" });
        }

        // Yêu cầu xóa sạp hàng từ Vendor
        [HttpPut("vendor/request-delete/{id}")]
        public async Task<IActionResult> RequestDeletePOI(int id)
        {
            var p = await _context.POIs.FindAsync(id);
            if (p == null) return NotFound();

            p.Status = "PendingDeleteApproval";
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã gửi yêu cầu xóa, chờ duyệt" });
        }
    }

    public class CheckoutRequest
    {
        public int VendorId { get; set; }
        public List<int> PoiIds { get; set; } = new List<int>();
    }
}