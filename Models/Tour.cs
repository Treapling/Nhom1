using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nhom1.Models
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên Tour không được để trống")]
        [MaxLength(200)]
        public string Name { get; set; }

        // Navigation Property: 1 Tour chứa nhiều POI
        public ICollection<POI> POIs { get; set; }
    }
}