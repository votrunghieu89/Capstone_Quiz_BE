using System.ComponentModel.DataAnnotations;

namespace Capstone.DTOs.RecruiterProfile
{
    public class RecruiterProfileUpdateJDDTO
    {
        [Required]
        public int JDId { get; set; }

        [MaxLength(200)]
        public string? JDTitle { get; set; }

        [MaxLength(200)]
        public string? JDSalary { get; set; }

        [MaxLength(200)]
        public string? JDLocation { get; set; }

        [MaxLength(200)]
        public string? JDExperience { get; set; }

        public DateTime? JDExpiredTime { get; set; }

        public string? Description { get; set; }
        public string? Requirement { get; set; }
        public string? Benefits { get; set; }
        public string? Location { get; set; }
        public string? WorkingTime { get; set; }
    }
}
