using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Position")]
    public class PositionModel
    {
        [Key]
        [Column("PositionId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PositionId { get; set; }

        [Column("PositionName")]
        [Required]
        [MaxLength(200)]
        public string PositionName { get; set; } = string.Empty;

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        public PositionModel() { }
        public PositionModel(string positionName, DateTime createAt, DateTime updateAt)
        {
            PositionName = positionName ?? string.Empty;
            CreatedAt = createAt;
            UpdatedAt = updateAt;
        }
    }
}