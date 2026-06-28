using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    public class POI
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        // --- BỔ SUNG ĐA NGÔN NGỮ MÔ TẢ ---
        public string DescriptionEn { get; set; } // Tiếng Anh
        public string DescriptionZh { get; set; } // Tiếng Trung
        public string DescriptionKo { get; set; } // Tiếng Hàn
        public string DescriptionJa { get; set; } // Tiếng Nhật
        // ---------------------------------

        [Required]
        public double Lat { get; set; } 

        [Required]
        public double Lng { get; set; } 

        [Required]
        public double Radius { get; set; } 

        [Required]
        public int Priority { get; set; } 

        // TRẠNG THÁI DUYỆT (Mới thêm)
        // 0: Chờ duyệt (Pending) | 1: Đã duyệt (Approved) | -1: Bị từ chối (Rejected)
        public int ApprovalStatus { get; set; } = 1; // Mặc định 1 để các data cũ không bị ẩn đi

        // Khóa ngoại liên kết với Vendor. Nếu UserId = null => POI này của Admin quản lý chung
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User Vendor { get; set; }

        public ICollection<Audio> Audios { get; set; }
        public ICollection<TrackingLog> TrackingLogs { get; set; } 
        public ICollection<Tour> Tours { get; set; } 
    }
}