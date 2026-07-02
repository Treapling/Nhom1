using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nhom1.Models
{
    /// <summary>
    /// [MODEL] Thực thể Tour - Đại diện cho một tuyến du lịch gồm nhiều địa điểm (POI)
    /// Quan hệ nhiều-nhiều: 1 Tour có nhiều POI, 1 POI có thể thuộc nhiều Tour
    /// </summary>
    public class Tour
    {
        [Key]
        public int Id { get; set; }                          // ID duy nhất của Tour

        [Required(ErrorMessage = "Tên Tour không được để trống")]
        [MaxLength(200)]
        public string Name { get; set; }                     // Tên Tour (vd: "Tour Ẩm thực Quận 4", "Tour Chợ nổi Miền Tây")

        /// <summary>
        /// Danh sách các địa điểm (POI) có trong tour này
        /// </summary>
        public ICollection<POI> POIs { get; set; }           // Navigation property: 1 Tour chứa nhiều POI
    }
}