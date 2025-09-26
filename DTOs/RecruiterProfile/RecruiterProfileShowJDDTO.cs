namespace Capstone.DTOs.RecruiterProfile
{
    public class RecruiterProfileShowJDDTO
    {
        public int PCId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string AvatarURL { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;

        public string JDTitle { get; set; } = string.Empty;

        public string JDSalary { get; set; } = string.Empty;

        public string JDLocation { get; set; } = string.Empty;

        public string JDExperience { get; set; } = string.Empty;

        public DateTime JDExpiredTime { get; set; } // sau khi model sua lai datetime thi sua lai datetime

        public string Description { get; set; } = string.Empty;

        public string Requirement { get; set; } = string.Empty;


        public string Benefits { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string WorkingTime { get; set; } = string.Empty;

        public List<string>? PositionName { get; set; }
    }
}
