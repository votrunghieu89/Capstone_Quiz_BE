using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("OnlineReports")]
    public class OnlineReportModel
    {
        [Key]
        [Column("OnlineReportId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OnlineReportId { get; set; }

        [Column("QuizId")]
        public int QuizId { get; set; }

        [Column("TeacherId")]
        public int TeacherId { get; set; }

        [Column("ReportName")]
        [Required]
        [MaxLength(100)]
        public string ReportName { get; set; } = string.Empty;

        [Column("HighestScore")]
        public int HighestScore { get; set; }

        [Column("LowestScore")]
        public int LowestScore { get; set; }

        [Column("AverageScore")]
        public decimal AverageScore { get; set; }

        [Column("TotalParticipants")]
        public int TotalParticipants { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}