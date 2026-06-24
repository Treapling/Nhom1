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

        [Required]
        public double Lat { get; set; } 

        [Required]
        public double Lng { get; set; } 

        [Required]
        public double Radius { get; set; } 

        [Required]
        public int Priority { get; set; } 

        // Khóa ngoại liên kết với Vendor. Nếu UserId = null => POI này của Admin quản lý chung
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User Vendor { get; set; }

        public ICollection<Audio> Audios { get; set; }
        public ICollection<TrackingLog> TrackingLogs { get; set; } 
        public ICollection<Tour> Tours { get; set; } // BẮT BUỘC PHẢI CÓ DÒNG NÀY ĐỂ KHÔNG LỖI
    }
}