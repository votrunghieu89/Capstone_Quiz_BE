using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("OfflineWrongAnswer")]
    public class OfflineWrongAnswerModule
    {
        [Key]
        [Column("OffWrongId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OffWrongId { get; set; }
        [ForeignKey("OfflineResultId")]
        public int OfflineResultId { get; set; }
        [Column("QuestionId")]
        public int QuestionId { get; set; }
        [Column("SelectedOptionId")]
        public int SelectedOptionId { get; set; }
        [Column("CorrectOptionId")]
        public int CorrectOptionId { get; set; }
        [Column("CreateAt")]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}
