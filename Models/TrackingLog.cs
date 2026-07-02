using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    /// <summary>
    /// [MODEL] Thực thể TrackingLog - Nhật ký theo dõi tương tác của người dùng với địa điểm
    /// Dùng để thống kê lượt quét QR, kích hoạt GPS, xem thực đơn, v.v.
    /// </summary>
    public class TrackingLog
    {
        [Key]
        public int Id { get; set; }                          // ID duy nhất của bản ghi tracking

        [Required]
        public int POI_Id { get; set; }                      // ID của địa điểm được tương tác

        [Required]
        public string EventType { get; set; }                // Loại sự kiện: "SCAN_QR" (quét mã QR) | "GPS_TRIGGER" (vào vùng geofence) | "VIEW_MENU" (xem thực đơn)

        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Thời điểm xảy ra sự kiện (theo UTC)

        /// <summary>
        /// Mã định danh phiên của khách vãng lai (SessionToken).
        /// Dùng để phân biệt người dùng độc lập và đếm số lượng user online chính xác.
        /// </summary>
        public string SessionToken { get; set; }

        [ForeignKey("POI_Id")]
        public POI POI { get; set; }                         // Liên kết đến địa điểm được tương tác
    }
}