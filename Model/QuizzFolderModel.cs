using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("QuizzFolders")]
    public class QuizzFolderModel
    {
        [Key]
        [Column("FolderId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FolderId { get; set; }

        [ForeignKey("TeacherId")]
        public int TeacherId { get; set; }
        [Column("FolderName")]
        [Required]
        [MaxLength(100)]
        public string FolderName { get; set; } = string.Empty;

        [Column("ParentFolderId")]
        public int? ParentFolderId { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
        [Column("UpdateAt")]
        public DateTime? UpdateAt { get; set; }
    }
}