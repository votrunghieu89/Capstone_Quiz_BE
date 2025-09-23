using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Capstone.DTOs.RecruiterProfile
{
    public class RecruiterProfileCreateJDDTO
    {
        [Required]
        public int PCId { get; set; }

        [Required]
        [MaxLength(200)]
        public string? JDTitle { get; set; }

        [Required]
        [MaxLength(200)]
        public string? JDSalary { get; set; }

        [Required]
        [MaxLength(200)]
        public string? JDLocation {get; set; }

        [Required]
        [MaxLength(200)]
        public string? JDExperience { get; set; }

        [Required]
        [MaxLength(200)]
        public string JDExpiredTime { get; set; } // sau khi model sua lai datetime thi sua lai datetime

        [Required]
        public string? Description { get; set; }

        [Required]
        public string? Requirement { get; set; }

        [Required]
        public string? Benefits { get; set; }

        [Required]
        public string? Location { get; set; }

        [Required]
        public string? WorkingTime { get; set; }


        
    }
}
