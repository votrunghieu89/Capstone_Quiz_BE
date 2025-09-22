using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("JDPosition")]
    public class JDPositionModel
    {
        [Key]
        [Column("JDPositionId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JDPositionId { get; set; }

        [Column("JDId")]
        public int JDId { get; set; }

        [Column("PositionId")]
        public int PositionId { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public JDPositionModel() { }
        public JDPositionModel(int jdId, int positionId, DateTime createAt)
        {
            JDId = jdId;
            PositionId = positionId;
            CreatedAt = createAt;
        }
    }
}
