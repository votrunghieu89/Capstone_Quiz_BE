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
        public string JDTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string JDSalary { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string JDLocation {get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string JDExperience { get; set; } = string.Empty;

        [Required]

        public DateTime JDExpiredTime { get; set; } // sau khi model sua lai datetime thi sua lai datetime

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Requirement { get; set; } = string.Empty;

        [Required]
        public string Benefits { get; set; } = string.Empty; 

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string WorkingTime { get; set; } = string.Empty;



    }
}
