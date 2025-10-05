using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("QuestionStats")]
    public class QuestionStatsModel
    {
        [Key]
        [Column("QuestionStatsId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuestionStatsId { get; set; }

        [Column("QuizId")]
        public int QuizId { get; set; }

        [Column("QuestionId")]
        public int? QuestionId { get; set; }

        [Column("CorrecCount")]
        public int? CorrecCount { get; set; }

        [Column("WrongCount")]
        public int? WrongCount { get; set; }

        [Column("TotalAnswer")]
        public int? TotalAnswer { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}