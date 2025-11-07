using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("OfflineWrongAnswers")]
    public class OfflineWrongAnswerModel
    {
        [Key]
        [Column("OffWrongId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OffWrongId { get; set; }

        [Column("OffResultId")]
        [ForeignKey("OfflineResult")]
        public int OffResultId { get; set; }

        [Column("QuestionId")]
        public int QuestionId { get; set; }

        [Column("SelectedOptionId")]
        public int? SelectedOptionId { get; set; }

        [Column("CorrectOptionId")]
        public int? CorrectOptionId { get; set; }

        [Column("CreateAt")]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}
