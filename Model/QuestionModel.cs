using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Questions")]
    public class QuestionModel
    {
        [Key]
        [Column("QuestionId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuestionId { get; set; }

        [ForeignKey("Quiz")]       // <-- chỉ định FK
        public int QuizId { get; set; }

        public QuizModel Quiz { get; set; }

        [Column("QuestionType")]
        [Required]
        [MaxLength(10)]
        public string QuestionType { get; set; } = string.Empty;

        [Column("QuestionContent")]
        [Required]
        public string QuestionContent { get; set; } = string.Empty;

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        [Column("UpdateAt")]
        public DateTime? UpdateAt { get; set; }
        [Column("Time")]
        public int Time { get; set; }

        public List<OptionModel> Options { get; set; } = new List<OptionModel>();
    }
}