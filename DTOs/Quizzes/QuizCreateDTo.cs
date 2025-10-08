namespace Capstone.DTOs.Quizzes
{
    public class QuizCreateDTo
    {
        public int TeacherId { get; set; }
        public int FolderId { get; set; }
        public int? TopicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public string? AvatarURL { get; set; }
        public int NumberOfPlays { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuestionDTO> Questions { get; set; } = new();
    }

    public class QuestionDTO
    {
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public int Time { get; set; }
       
        public List<OptionDTO> Options { get; set; } = new();
    }
    public class OptionDTO
    {
        public string OptionContent { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
      
    }
}
