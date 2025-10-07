using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Quizzes")]
    public class QuizModel
    {
        [Key]
        [Column("QuizId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuizId { get; set; }

        [ForeignKey("TeacherId")]
        public int TeacherId { get; set; }

        [ForeignKey("FolderId")]
        public int FolderId { get; set; }

        [ForeignKey("TopicId")]
        public int? TopicId { get; set; }

        [Column("Title")]
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Column("Description")]
        [Required]
        public string Description { get; set; } = string.Empty;

        [Column("IsPrivate")]
        public bool IsPrivate { get; set; }

        [Column("AvartarURL")]
        [MaxLength(255)]
        public string? AvartarURL { get; set; }
        [Column("TotalParticipants")]
        public int TotalParticipants { get; set; } = 0;
        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        [Column("UpdateAt")]
        public DateTime? UpdateAt { get; set; }

        public List<QuestionModel> Questions { get; set; } =  new List<QuestionModel>();
    }
}