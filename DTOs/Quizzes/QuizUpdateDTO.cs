namespace Capstone.DTOs.Quizzes
{
    public class QuizUpdateDTO
    {
        public int QuizId { get; set; }
        public int FolderId { get; set; }
        public int? TopicId { get; set; }
        public int? GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public string? AvartarURL { get; set; }
        public DateTime UpdateAt { get; set; } = DateTime.Now;
        public List<QuestionUpdateDTO> Questions { get; set; } = new();
    }

    public class QuestionUpdateDTO
    {
        public int? QuestionId { get; set; } // null nếu là câu mới
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public int Time { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime UpdateAt { get; set; } = DateTime.Now;
        public List<OptionUpdateDTO> Options { get; set; } = new();
    }

    public class OptionUpdateDTO
    {
        public int? OptionId { get; set; } // null nếu là option mới
        public string OptionContent { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime UpdateAt { get; set; } = DateTime.Now;
    }
}
