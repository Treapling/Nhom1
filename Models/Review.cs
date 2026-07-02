using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    /// <summary>
    /// [MODEL] Thực thể Đánh giá (Review) - Lưu đánh giá và bình luận của khách tham quan về địa điểm
    /// </summary>
    public class Review
    {
        [Key]
        public int Id { get; set; }                          // ID duy nhất của đánh giá

        [Required]
        public int POI_Id { get; set; }                      // ID của địa điểm được đánh giá

        [Required]
        public int Rating { get; set; }                      // Số sao đánh giá (từ 1 đến 5)

        public string Comment { get; set; }                  // Nội dung bình luận (GuestFree bị xóa trường này)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Thời gian tạo đánh giá (theo UTC)

        [ForeignKey("POI_Id")]
        public POI POI { get; set; }                         // Liên kết đến địa điểm được đánh giá
    }
}