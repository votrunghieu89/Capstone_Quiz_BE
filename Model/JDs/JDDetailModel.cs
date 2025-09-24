using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("JDDetail")]
    public class JDDetailModel
    {
        [Key]
        [Column("JDDetailId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JDDetailId { get; set; }

        [Column("JDId")]
        public int JDId { get; set; }

        [Column("Description")]
        public string? Description { get; set; }

        [Column("Requirement")]
        public string? Requirement { get; set; }

        [Column("Benefits")]
        public string? Benefits { get; set; }

        [Column("Location")]
        public string? Location { get; set; }

        [Column("WorkingTime")]
        public string? WorkingTime { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        public JDDetailModel() { }
        public JDDetailModel(int jdId, string? description, string? requirement,
            string? benefits, string? location, string? workingTime,
            DateTime createAt, DateTime updateAt)
        {
            JDId = jdId;
            Description = description;
            Requirement = requirement;
            Benefits = benefits;
            Location = location;
            WorkingTime = workingTime;
            CreatedAt = createAt;
            UpdatedAt = updateAt;
        }
    }
}