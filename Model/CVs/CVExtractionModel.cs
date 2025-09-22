using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("CVExtraction")]
    public class CVExtractionModel
    {
        [Key]
        [Column("ExtractionId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExtractionId { get; set; }

        [Column("CVId")]
        public int CVId { get; set; }

        [Column("School")]
        [Required]
        [MaxLength(255)]
        public string School { get; set; } = string.Empty;

        [Column("Skills")]
        [Required]
        public string Skills { get; set; } = string.Empty;

        [Column("Certifications")]
        [Required]
        public string Certifications { get; set; } = string.Empty;

        [Column("Experiences")]
        public int Experiences { get; set; }

        [Column("GPA", TypeName = "decimal(5,2)")]
        public decimal GPA { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CVExtractionModel() { }
        public CVExtractionModel(int cvId, string school, string skills, string certifications, int experiences, decimal gpa)
        {
            CVId = cvId;
            School = school ?? string.Empty;
            Skills = skills ?? string.Empty;
            Certifications = certifications ?? string.Empty;
            Experiences = experiences;
            GPA = gpa;
            CreatedAt = DateTime.UtcNow;
        }
    }
}