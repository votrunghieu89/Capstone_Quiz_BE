using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("OnlineWrongAnswer")]
    public class OnlineWrongAnswerModel
    {
        [Key]
        [Column("OnlWrongId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OnlWrongId { get; set; }

        [Column("OnlResultId")]
        public int OnlResultId { get; set; }

        [Column("QuestionId")]
        public int QuestionId { get; set; }

        [Column("SelectedOptionId")]
        public int? SelectedOptionId { get; set; }

        [Column("CorrectOptionId")]
        public int? CorrectOptionId { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}