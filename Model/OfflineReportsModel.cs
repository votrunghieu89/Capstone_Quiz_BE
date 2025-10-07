using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("OfflineReports")]
    public class OfflineReportsModel
    {
        [Key]
        [Column("ReportId")]
        public int ReportId { get; set; }
        [ForeignKey("QGId")]
        public int QGId { get; set; }
        [Column("QuizId")]
        public int QuizId { get; set; }
        [Column("ReportName")]
        [Required]
        [MaxLength(200)]
        public string ReportName { get; set; }
        [Column("HighestScore")]
        public int HighestScore { get; set; }
        [Column("LowestScore")]
        public int LowestScore { get; set; }
        [Column("AverageScore")]
        public decimal AverageScore { get; set; }
        [Column("TotalParticipants")]
        public int TotalParticipants { get; set; }
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
