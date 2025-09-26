using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Capstone.DTOs.RecruiterProfile
{
    public class RecruiterProfileCreateJDDTO
    {
        public int PCId { get; set; }

        public string JDTitle { get; set; } = string.Empty;

        public string JDSalary { get; set; } = string.Empty;

        public string JDLocation {get; set; } = string.Empty;

        public string JDExperience { get; set; } = string.Empty;

        public DateTime JDExpiredTime { get; set; } // sau khi model sua lai datetime thi sua lai datetime

        public string Description { get; set; } = string.Empty;

        public string Requirement { get; set; } = string.Empty;


        public string Benefits { get; set; } = string.Empty; 

        public string Location { get; set; } = string.Empty;

        public string WorkingTime { get; set; } = string.Empty;

        public List<int>? PositionIds { get; set; } 

        public RecruiterProfileCreateJDDTO() { }

        public RecruiterProfileCreateJDDTO(int pcId, string jdTitle, string jdSalary,
            string jdLocation, string jdExperience, DateTime jdExpiredTime, string description,
            string requirement, string benefits, string location, string workingTime, List<int>? positionIds)
        {
            PCId = pcId;
            JDTitle = jdTitle;
            JDSalary = jdSalary;
            JDLocation = jdLocation;
            JDExperience = jdExperience;
            JDExpiredTime = jdExpiredTime;
            Description = description;
            Requirement = requirement;
            Benefits = benefits;
            Location = location;
            WorkingTime = workingTime;
            PositionIds = positionIds;
        }

    }
}
