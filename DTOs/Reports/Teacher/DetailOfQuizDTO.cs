namespace Capstone.DTOs.Reports.Teacher
{
    public class DetailOfQuizDTO
    {
        public string GroupName { get; set; }
        public int TotalParticipants { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public decimal AverageScore { get; set; }
        public int TotalQuestions { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CreateBy  { get; set; } = string.Empty;

    }
}
