using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("TeacherProfile")]
    public class TeacherProfileModel
    {
        [Key]
        [ForeignKey("TeacherId")]
        public int TeacherId { get; set; } // FK to Accounts.AccountId

        [Column("FullName")]
        [Required]
        [MaxLength(100)]
        public string? FullName { get; set; } 

        [Column("PhoneNumber")]
        [MaxLength(100)]
        public string? PhoneNumber { get; set; }

        [Column("AvatarURL")]
        [MaxLength(100)]
        public string? AvatarURL { get; set; }

        [Column("IdUnique")]
        public string IdUnique { get; set; } 

        [Column("OrganizationName")]
        [MaxLength(100)]
        public string? OrganizationName { get; set; } 

        [Column("OrganizationAddress")]
        [MaxLength(100)]
        public string? OrganizationAddress { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        [Column("UpdateAt")]
        public DateTime? UpdateAt { get; set; }
    }
}