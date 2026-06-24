using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    public class VendorProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Liên kết 1-1 với bảng Users

        public string AvatarUrl { get; set; }
        
        public bool IsFoodSafetyCertified { get; set; } = false; // Chứng nhận ATTP

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}