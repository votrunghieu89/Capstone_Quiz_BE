namespace Capstone.DTOs
{
    public class SearchStudentInOfflineReportDTO
    {
        public string Fullname { get; set; }
        public int? Rank { get; set; }
        public int NumberOfCorrectAnswers { get; set; }
        public int NumberOfWrongAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int FinalScore { get; set; }
        public int CountAttempts { get; set; }
    }
}
