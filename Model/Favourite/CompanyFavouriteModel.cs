using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("CompanyFavourite")]
    public class CompanyFavouriteModel
    {
        [Key]
        [Column("CompanyFavouriteId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CompanyFavouriteId { get; set; }

        [Column("PCAId")]
        public int PCAId { get; set; }

        [Column("PCId")]
        public int PCId { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CompanyFavouriteModel() { }
        public CompanyFavouriteModel(int pcaId, int pcId)
        {
            PCAId = pcaId;
            PCId = pcId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}