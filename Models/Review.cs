using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nhom1.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int POI_Id { get; set; }
        [Required]
        public int Rating { get; set; } // 1 đến 5 sao
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("POI_Id")]
        public POI POI { get; set; }
    }
}