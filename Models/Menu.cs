using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    public class Menu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int POI_Id { get; set; }

        [Required, MaxLength(150)]
        public string ItemName { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        [ForeignKey("POI_Id")]
        public POI POI { get; set; }
    }
}