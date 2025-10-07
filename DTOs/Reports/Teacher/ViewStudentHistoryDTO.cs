namespace Capstone.DTOs.Reports.Teacher
{
    public class ViewStudentHistoryDTO
    {
        public string Fullname { get; set; } = string.Empty;
        public int Rank { get; set; }
        public int NumberOfCorrectAnswers { get; set; }
        public int NumberOfWrongAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int FinalScore { get; set; }
    }
}
