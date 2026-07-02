using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    /// <summary>
    /// [MODEL] Thực thể Địa điểm (Point of Interest - POI)
    /// Đại diện cho một địa điểm du lịch, quán ăn, điểm tham quan trên bản đồ
    /// </summary>
    public class POI
    {
        [Key]
        public int Id { get; set; }                          // ID duy nhất của địa điểm

        [Required, MaxLength(200)]
        public string Name { get; set; }                     // Tên địa điểm (vd: "Quán Ốc Vũ", "Chợ Bến Thành")

        [MaxLength(2000)]
        public string Description { get; set; }              // Mô tả địa điểm bằng tiếng Việt

        // --- MÔ TẢ ĐA NGÔN NGỮ ---
        public string DescriptionEn { get; set; }            // Mô tả bằng tiếng Anh
        public string DescriptionZh { get; set; }            // Mô tả bằng tiếng Trung
        public string DescriptionKo { get; set; }            // Mô tả bằng tiếng Hàn
        public string DescriptionJa { get; set; }            // Mô tả bằng tiếng Nhật
        // -------------------------

        [Required]
        public double Lat { get; set; }                      // Vĩ độ (Latitude) - tọa độ GPS

        [Required]
        public double Lng { get; set; }                      // Kinh độ (Longitude) - tọa độ GPS

        [Required]
        public double Radius { get; set; }                   // Bán kính vùng geofence (tính bằng mét)

        [Required]
        public int Priority { get; set; }                    // Độ ưu tiên hiển thị (số càng cao càng ưu tiên)

        /// <summary>
        /// Trạng thái duyệt địa điểm:
        /// 0 = Chờ duyệt (Pending)
        /// 1 = Đã duyệt (Approved) - mặc định
        /// -1 = Bị từ chối (Rejected)
        /// </summary>
        public int ApprovalStatus { get; set; } = 1;

        /// <summary>
        /// Khóa ngoại liên kết với User (Vendor).
        /// Nếu null => POI này do Admin quản lý chung.
        /// </summary>
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User Vendor { get; set; }                     // Vendor sở hữu POI này

        // Navigation properties - Liên kết đến các bảng liên quan
        public ICollection<Audio> Audios { get; set; }       // Danh sách file audio của địa điểm
        public ICollection<TrackingLog> TrackingLogs { get; set; } // Lịch sử tương tác (quét QR, GPS)
        public ICollection<Tour> Tours { get; set; }         // Các tour du lịch có chứa địa điểm này
    }
}