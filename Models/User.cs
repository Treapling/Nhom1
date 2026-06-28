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
        public string PasswordHash { get; set; } 

        [Required]
        public string Role { get; set; } 

        public string ShopName { get; set; } 
        public string ContactInfo { get; set; } 
        public bool IsActive { get; set; } = true; 

        // GIỚI HẠN SLOT MỞ QUÁN: Đổi về 0 để bắt buộc mua Slot ngay từ quán đầu tiên
        public int MaxPOISlots { get; set; } = 0; 
    }
}