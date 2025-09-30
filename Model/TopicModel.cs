using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Topics")]
    public class TopicModel
    {
        [Key]
        [Column("TopicId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TopicId { get; set; }

        [Column("TopicName")]
        [Required]
        [MaxLength(100)]
        public string TopicName { get; set; } = string.Empty;

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}