using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("OnlineResults")]
    public class OnlineResultModel
    {
        [Key]
        [Column("OnlResultId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OnlResultId { get; set; }

        [ForeignKey("QuizId")]
        public int QuizId { get; set; }
        [Column("OnlineReportId")]
        public int OnlineReportId { get; set; }
        [Column("StudentName")]
        [Required]
        [MaxLength(50)]
        public string StudentName { get; set; } = string.Empty;

        [Column("Score")]
        public int Score { get; set; }

        [Column("CorrectCount")]
        public int? CorrecCount { get; set; }

        [Column("WrongCount")]
        public int? WrongCount { get; set; }
        [Column("TotalQuestion")]
        public int? TotalQuestion { get; set; }
        [Column("Rank")]
        public int? Rank { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}