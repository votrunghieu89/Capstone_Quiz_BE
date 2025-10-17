namespace Capstone.DTOs
{
    public class OfflineResultViewDTO
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int CountAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalQuestion { get; set; }
        public int Score { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Duration { get; set; }
    }
    public class StartOfflineQuizDTO
    {
        public int StudentId { get; set; }
        public int QGId { get; set; } // Quizz_Group Id
    }

    public class FinishOfflineQuizDTO
    {
        public int StudentId { get; set; }
        public int QGId { get; set; }
        public int QuizId { get; set; }
    }

    public class OfflineQuizCacheDTO
    {
        public int QuizId { get; set; }
        public int StudentId { get; set; }
        public int NumberOfCorrectAnswer { get; set; }
        public int NumberOfWrongAnswer { get; set; }
        public int TotalQuestion { get; set; }
        public List<WrongAnswerDTO> WrongAnswers { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Duration { get; set; }
    }

    public class WrongAnswerDTO
    {
        public int QuestionId { get; set; }
        public int SelectedOptionId { get; set; }
        public int CorrectOptionId { get; set; }
    }
}
