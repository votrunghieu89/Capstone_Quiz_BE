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

        [Column("QuizId")]
        public int QuizId { get; set; }

        [Column("GroupId")]
        public int GroupId { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}