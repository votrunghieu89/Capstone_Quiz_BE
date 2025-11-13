namespace Capstone.DTOs.Quizzes.QuizzOnline
{
    public class StudentCompleteResultDTO
    {
        public string StudentName { get; set; }
        public int Score { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int Rank { get; set; }
        public int TotalQuestions { get; set; }
        public List<QuestionResultDTO> Questions { get; set; }
    }
    public class QuestionResultDTO
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; }
        public List<OptionResultDTO> Options { get; set; }
        public bool? IsSkipped { get; set; }
    }
    public class OptionResultDTO
    {
        public int OptionId { get; set; }
        public string OptionContent { get; set; }
        public bool IsCorrect { get; set; } // đáp án đúng
        public bool IsSelectedWrong { get; set; } // học sinh chọn sai
    }
}
