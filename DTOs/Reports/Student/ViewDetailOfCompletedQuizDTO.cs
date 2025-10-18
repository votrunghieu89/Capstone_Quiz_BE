using Capstone.DTOs.Quizzes;

namespace Capstone.DTOs.Reports.Student
{
    public class ViewDetailOfCompletedQuizDTO
    {
        public string QuizTitle { get; set; }
        public int NumberOfCorrectAnswers { get; set; }
        public int NumberOfWrongAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int FinalScore { get; set; }
        public int? Rank { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime CompletedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public List<QuestionDetailDTO> QuestionDetails { get; set; } = new List<QuestionDetailDTO>();
    }
    public class QuestionDetailDTO
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; } = string.Empty;
        public int Time { get; set; }

        public List<OptionDetailDTO> Answers { get; set; } = new List<OptionDetailDTO>();
    }
    public class OptionDetailDTO
    {
        public int OptionId { get; set; }
        public string OptionContent { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }
    }
}
