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

        [Column("StudentName")]
        [Required]
        [MaxLength(50)]
        public string StudentName { get; set; } = string.Empty;

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