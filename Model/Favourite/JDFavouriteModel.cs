using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("JDFavourite")]
    public class JDFavouriteModel
    {
        [Key]
        [Column("FavouriteId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FavouriteId { get; set; }

        [Column("PCAId")]
        public int PCAId { get; set; }

        [Column("JDId")]
        public int JDId { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public JDFavouriteModel() { }
        public JDFavouriteModel(int pcaId, int jdId, DateTime createdAt)
        {
            PCAId = pcaId;
            JDId = jdId;
            CreatedAt = createdAt;
        }
    }
}