using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    /// <summary>
    /// [MODEL] Thực thể Menu - Lưu thông tin món ăn / sản phẩm của từng địa điểm (quán)
    /// Mỗi POI (quán) có thể có nhiều món trong menu
    /// </summary>
    public class Menu
    {
        [Key]
        public int Id { get; set; }                          // ID duy nhất của món ăn

        [Required]
        public int POI_Id { get; set; }                      // ID của địa điểm (quán) mà món này thuộc về

        [Required, MaxLength(150)]
        public string ItemName { get; set; }                 // Tên món ăn (vd: "Bún bò Huế", "Cà phê sữa đá")

        public string Description { get; set; }              // Mô tả chi tiết món ăn

        [Required]
        public decimal Price { get; set; }                   // Giá tiền của món ăn

        public string ImageUrl { get; set; }                 // Đường dẫn hình ảnh món ăn

        [ForeignKey("POI_Id")]
        public POI POI { get; set; }                         // Liên kết đến địa điểm (quán) cha
    }
}