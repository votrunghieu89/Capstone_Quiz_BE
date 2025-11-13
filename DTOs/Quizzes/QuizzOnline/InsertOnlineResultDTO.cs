namespace Capstone.DTOs.Quizzes.QuizzOnline
{
    public class InsertOnlineReportDTO
    {
        public int QuizId { get; set; }
        public int TeacherId { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public decimal AverageScore { get; set; }
        public int TotalParticipants { get; set; }
        public List<InsertOnlineResultDTO> InsertOnlineResultDTO { get; set; } = new List<InsertOnlineResultDTO>();
    }
    public class InsertOnlineResultDTO
    {
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalQuestions { get; set; }
        public int Rank { get; set; }
        public List<InsertWrongAnswerDTO> wrongAnswerDTOs { get; set; } = new List<InsertWrongAnswerDTO>();
    }
    public class InsertWrongAnswerDTO
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public int? CorrectOptionId { get; set; }
    }
}
