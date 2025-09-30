using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Student_Group")]
    public class StudentGroupModel
    {
        [Key]
        [Column("SGId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SGId { get; set; }

        [Column("StudentId")]
        public int StudentId { get; set; }

        [Column("GroupId")]
        public int GroupId { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}