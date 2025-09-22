using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("JDs")]
    public class JDsModel
    {
        [Key]
        [Column("JDId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JDId { get; set; }

        [Column("PCId")]
        public int PCId { get; set; }

        [Column("JDTitle")]
        [Required]
        [MaxLength(200)]
        public string JDTitle { get; set; } = string.Empty;

        [Column("JDSalary")]
        [MaxLength(200)]
        public string JDSalary { get; set; } = string.Empty;

        [Column("JDLocation")]
        [Required]
        [MaxLength(200)]
        public string JDLocation { get; set; } = string.Empty;

        [Column("JDExperience")]
        [Required]
        [MaxLength(200)]
        public string JDExperience { get; set; } = string.Empty;

        [Column("JDExpiredTime")]
        [Required]
        [MaxLength(200)]
        public string JDExpiredTime { get; set; } = string.Empty;

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        public JDsModel() { }
        public JDsModel(int pcId, string jdTitle, string jdSalary, string jdLocation,
            string jdExperience, string jdExpiredTime, DateTime createAt, DateTime updateAt)
        {
            PCId = pcId;
            JDTitle = jdTitle ?? string.Empty;
            JDSalary = jdSalary ?? string.Empty;
            JDLocation = jdLocation ?? string.Empty;
            JDExperience = jdExperience ?? string.Empty;
            JDExpiredTime = jdExpiredTime ?? string.Empty;
            CreatedAt = createAt;
            UpdatedAt = updateAt;
        }
    }
}