using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("CV_JD_Apply")]
    public class CV_JD_ApplyModel
    {
        [Key]
        [Column("ApplyId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ApplyId { get; set; }

        [Column("CVId")]
        public int CVId { get; set; }

        [Column("JDId")]
        public int JDId { get; set; }

        [Column("Status")]
        [MaxLength(10)]
        public string? Status { get; set; }

        [Column("ReviewedDate")]
        public DateTime? ReviewedDate { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CV_JD_ApplyModel() { }
        public CV_JD_ApplyModel(int cvId, int jdId, string? status, DateTime? reviewedDate)
        {
            CVId = cvId;
            JDId = jdId;
            Status = status;
            ReviewedDate = reviewedDate;
            CreatedAt = DateTime.UtcNow;
        }
    }
}