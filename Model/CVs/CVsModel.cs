using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("CVs")]
    public class CVsModel
    {
        [Key]
        [Column("CVId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CVId { get; set; }

        [Column("PCAId")]
        public int PCAId { get; set; }

        [Column("FileName")]
        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Column("FilePath")]
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public CVsModel() { }
        public CVsModel(int pcaId, string fileName, string filePath)
        {
            PCAId = pcaId;
            FileName = fileName ?? string.Empty;
            FilePath = filePath ?? string.Empty;
            CreatedAt = DateTime.Now;
        }

    }
}