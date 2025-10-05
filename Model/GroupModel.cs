using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Groups")]
    public class GroupModel
    {
        [Key]
        [Column("GroupId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GroupId { get; set; }

        [ForeignKey("TeacherId")]
        public int TeacherId { get; set; }

        [Column("GroupName")]
        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; } = string.Empty;

        [Column("GroupDescription")]
        [MaxLength(255)]
        public string? GroupDescription { get; set; }
        [Column("IdUnique")]
        public string IdUnique { get; set; } = string.Empty;

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}   