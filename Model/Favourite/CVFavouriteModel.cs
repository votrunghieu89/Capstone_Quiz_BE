using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("CVFavourite")]
    public class CVFavouriteModel
    {
        [Key]
        [Column("CVFavouriteId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CVFavouriteId { get; set; }

        [Column("PCId")]
        public int PCId { get; set; }

        [Column("CVId")]
        public int CVId { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CVFavouriteModel() { }
        public CVFavouriteModel(int pcId, int cvId)
        {
            PCId = pcId;
            CVId = cvId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}