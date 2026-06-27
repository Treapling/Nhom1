using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nhom1.Models
{
    public class POI
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên địa điểm không được để trống")]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [Required]
        public double Lat { get; set; } // Vĩ độ

        [Required]
        public double Lng { get; set; } // Kinh độ

        [Required]
        public double Radius { get; set; } // Bán kính kích hoạt (tính bằng mét)

        [Required]
        public int Priority { get; set; } // Mức ưu tiên xử lý chống spam

        public int? VendorId { get; set; } // ID của người bán hàng (nếu có)
        
        [MaxLength(50)]
        public string Status { get; set; } = "Approved"; // "PendingPayment", "PendingApproval", "Approved", "Rejected", "PendingEditApproval", "PendingDeleteApproval"

        public string? PendingChanges { get; set; } // Lưu JSON chứa thông tin sửa đổi chờ duyệt

        // Navigation Properties: 1 Điểm có thể có nhiều file Audio và nằm trong nhiều Tour
        public ICollection<Audio> Audios { get; set; }
        public ICollection<Tour> Tours { get; set; }
    }
}