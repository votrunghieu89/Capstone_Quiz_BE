using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Quizz_Group")]
    public class QuizzGroupModel
    {
        [Key]
        [Column("QGId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QGId { get; set; }

        [ForeignKey("QuizId")]
        public int QuizId { get; set; }

        [ForeignKey("GroupId")]
        public int GroupId { get; set; }
        [Column("Message")]
        public string? Message { get; set; }
        [Column("Status")]
        public string Status { get; set; } = string.Empty;
        [Column("ExpiredTime")]
        public DateTime ExpiredTime { get; set; }
        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}