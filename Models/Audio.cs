using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Nhom1.Models
{
    /// <summary>
    /// [MODEL] Thực thể Audio - Lưu thông tin file âm thanh hướng dẫn cho từng địa điểm
    /// Mỗi POI có thể có nhiều file audio với các ngôn ngữ khác nhau
    /// </summary>
    public class Audio
    {
        [Key]
        public int Id { get; set; }                          // ID duy nhất của file audio

        [Required]
        [JsonPropertyName("POI_Id")]
        public int POI_Id { get; set; }                      // ID của địa điểm (POI) mà audio này thuộc về

        [ForeignKey("POI_Id")]
        public POI POI { get; set; }                         // Liên kết đến địa điểm cha

        [Required(ErrorMessage = "Đường dẫn file không được để trống")]
        [MaxLength(500)]
        public string FilePath { get; set; }                 // Đường dẫn file audio (vd: "/audios/1_vi_123456789.mp3")

        [Required]
        [MaxLength(50)]
        public string Language { get; set; }                 // Ngôn ngữ: "vi" (Tiếng Việt), "en" (English), "zh" (中文), "ko" (한국어), "ja" (日本語)

        /// <summary>
        /// Phân loại gói audio:
        /// false = Audio Thường (GuestFree nghe được)
        /// true  = Audio Premium (chỉ GuestPremium mới nghe được)
        /// </summary>
        public bool IsPremium { get; set; } = false;
    }
}