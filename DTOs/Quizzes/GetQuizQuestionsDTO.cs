namespace Capstone.DTOs.Quizzes
{
    public class GetQuizQuestionsDTO
    {
        public int QuestionId { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public int Time { get; set; }
        
        public List<GetOptionDTO> Options { get; set; } = new();
    }
    public class GetOptionDTO
    {
        public int OptionId { get; set; }
        public string OptionContent { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
