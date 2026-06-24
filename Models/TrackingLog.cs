using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    public class TrackingLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int POI_Id { get; set; }

        [Required]
        public string EventType { get; set; } // Ví dụ: "SCAN_QR", "GPS_TRIGGER", "VIEW_MENU"

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Tùy chọn: Lưu SessionId của khách vãng lai để phân biệt người dùng độc lập
        public string SessionToken { get; set; } 

        [ForeignKey("POI_Id")]
        public POI POI { get; set; }
    }
}