using System.ComponentModel.DataAnnotations;

namespace Nhom1.Models
{
    /// <summary>
    /// [MODEL] Thực thể Người dùng - Lưu thông tin tài khoản đăng nhập và phân quyền
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }                          // ID duy nhất của tài khoản

        [Required, MaxLength(100)]
        public string Username { get; set; }                 // Tên đăng nhập (duy nhất trong hệ thống)

        [Required]
        public string PasswordHash { get; set; }             // Mật khẩu (dạng plain text - cần cải thiện bảo mật)

        [Required]
        public string Role { get; set; }                     // Vai trò: "Admin" | "Vendor" | "GuestFree" | "GuestPremium"

        public string ShopName { get; set; }                 // Tên quán (chỉ Vendor mới có)
        public string ContactInfo { get; set; }              // Thông tin liên hệ của Vendor
        public bool IsActive { get; set; } = true;           // Trạng thái hoạt động: true = đang hoạt động, false = bị khóa

        /// <summary>
        /// Số lượng địa điểm (POI) tối đa mà Vendor được đăng ký.
        /// Mặc định = 0 => bắt buộc phải mua slot mới được đăng ký quán.
        /// </summary>
        public int MaxPOISlots { get; set; } = 0;
    }
}