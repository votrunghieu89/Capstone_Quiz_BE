using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Options")]
    public class OptionModel
    {
        [Key]
        [Column("OptionId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OptionId { get; set; }


        [ForeignKey("Question")]
        public int QuestionId { get; set; }

        public QuestionModel Question { get; set; }

        [Column("OptionContent")]
        [Required]
        public string OptionContent { get; set; } = string.Empty;

        [Column("IsCorrect")]
        public bool IsCorrect { get; set; }
        [Column("IsDeleted")]
        public bool IsDeleted { get; set; } = false;
    }
}