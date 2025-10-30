namespace Capstone.DTOs.Quizzes
{
    public class ViewDetailDTO
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AvatarURL { get; set; }
        public int? TotalParticipants { get; set; }
        public int? TotalQuestions { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<QuestionDetailDTO> Questions { get; set; } = new List<QuestionDetailDTO>();
    }

    public class QuestionDetailDTO
    {
        public int QuestionId { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public int Time { get; set; }
        public int Score { get; set; }
        public List<OptionDetailDTO> Options { get; set; } = new List<OptionDetailDTO>();
    }

    public class OptionDetailDTO
    {
        public int OptionId { get; set; }
        public string OptionContent { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
