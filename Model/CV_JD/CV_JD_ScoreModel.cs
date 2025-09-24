using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("CV_JD_Score")]
    public class CV_JD_ScoreModel
    {
        [Key]
        [Column("ScoreId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ScoreId { get; set; }

        [Column("CVId")]
        public int CVId { get; set; }

        [Column("JDId")]
        public int JDId { get; set; }

        [Column("Score", TypeName = "decimal(5,2)")]
        public decimal Score { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public CV_JD_ScoreModel() { }
        public CV_JD_ScoreModel(int cvId, int jdId, decimal score)
        {
            CVId = cvId;
            JDId = jdId;
            Score = score;
            CreatedAt = DateTime.Now;
        }

    }
}