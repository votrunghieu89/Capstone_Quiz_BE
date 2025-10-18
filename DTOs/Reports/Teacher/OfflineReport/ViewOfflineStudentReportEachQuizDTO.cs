namespace Capstone.DTOs.Reports.Teacher.OfflineReport
{
    public class ViewOfflineStudentReportEachQuizDTO
    {
        public string Fullname { get; set; } = string.Empty;
        public int? Rank { get; set; }
        public int NumberOfCorrectAnswers { get; set; }
        public int NumberOfWrongAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int FinalScore { get; set; }
        public int CountAttempts { get; set; }
    }
}
