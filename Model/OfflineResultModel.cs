using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("OfflineResults")]
    public class OfflineResultModel
    {
        [Key]
        [Column("OffResultId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OffResultId { get; set; }
        [ForeignKey("StudentId")]
        public int StudentId { get; set; }
        [Column("QuizId")]
        public int QuizId { get; set; }
        [Column("CorrecCount")]
        public int? CorrecCount { get; set; }
        [Column("WrongCount")]
        public int? WrongCount { get; set; }
        [Column("TotalQuestion")]
        public int? TotalQuestion { get; set; }
        [Column("StartDate")]
        public DateTime? StartDate { get; set; }
        [Column("EndDate")]
        public DateTime? EndDate { get; set; }
        [Column("Duration")]
        public int? Duration { get; set; } // Duration in minutes
        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}