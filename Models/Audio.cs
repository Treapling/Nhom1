using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    public class Audio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int POI_Id { get; set; }

        [ForeignKey("POI_Id")]
        public POI POI { get; set; } 

        [Required(ErrorMessage = "Đường dẫn file không được để trống")]
        [MaxLength(500)]
        public string FilePath { get; set; }

        [Required]
        [MaxLength(50)]
        public string Language { get; set; } // Hỗ trợ đa ngôn ngữ như slide yêu cầu
    }
}