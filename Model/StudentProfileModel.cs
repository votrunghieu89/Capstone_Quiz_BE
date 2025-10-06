using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("StudentProfile")]
    public class StudentProfileModel
    {
        [Key]
        [ForeignKey("StudentId")]
        public int StudentId { get; set; } // FK to Accounts.AccountId

        [Column("FullName")]
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Column("AvatarURL")]
        [MaxLength(100)]
        public string? AvatarURL { get; set; }

        [Column("IdUnique")]
        public string IdUnique { get; set; } = string.Empty;

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        [Column("UpdateAt")]
        public DateTime? UpdateAt { get; set; }
    }
}