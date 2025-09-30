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

        [Column("StudentId")]
        public int StudentId { get; set; }

        [Column("QuizId")]
        public int QuizId { get; set; }

        [Column("Score")]
        public int Score { get; set; }

        [Column("CorrecCount")]
        public int? CorrecCount { get; set; }

        [Column("WrongCount")]
        public int? WrongCount { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}