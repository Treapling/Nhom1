using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    /// <summary>
    /// [MODEL] Thực thể Hồ sơ Vendor - Lưu thông tin mở rộng của chủ quán (liên kết 1-1 với User)
    /// </summary>
    public class VendorProfile
    {
        [Key]
        public int Id { get; set; }                          // ID duy nhất của hồ sơ Vendor

        [Required]
        public int UserId { get; set; }                      // ID của User (liên kết 1-1 với bảng Users)

        public string AvatarUrl { get; set; }                // Đường dẫn ảnh đại diện của Vendor

        /// <summary>
        /// Chứng nhận An toàn thực phẩm (ATTP)
        /// false = Chưa có chứng nhận
        /// true = Đã được chứng nhận ATTP
        /// </summary>
        public bool IsFoodSafetyCertified { get; set; } = false;

        [ForeignKey("UserId")]
        public User User { get; set; }                       // Liên kết đến tài khoản User của Vendor
    }
}