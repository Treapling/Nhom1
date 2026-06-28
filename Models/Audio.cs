using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Nhom1.Models
{
    public class Audio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [JsonPropertyName("POI_Id")]
        public int POI_Id { get; set; }

        [ForeignKey("POI_Id")]
        public POI POI { get; set; } 

        [Required(ErrorMessage = "Đường dẫn file không được để trống")]
        [MaxLength(500)]
        public string FilePath { get; set; }

        [Required]
        [MaxLength(50)]
        public string Language { get; set; }

        // MỚI: Phân loại nhạc Thường (false) và Nhạc Premium (true)
        public bool IsPremium { get; set; } = false;
    }
}