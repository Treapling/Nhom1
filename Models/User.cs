using System.ComponentModel.DataAnnotations;

namespace Nhom1.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Sẽ lưu mật khẩu mã hóa

        [Required]
        public string Role { get; set; } // "Admin" hoặc "Vendor"

        public string ShopName { get; set; } // Tên quán (Dành cho Vendor)
        public string ContactInfo { get; set; } 
        public bool IsActive { get; set; } = true; // Admin có quyền khóa tài khoản
    }
}