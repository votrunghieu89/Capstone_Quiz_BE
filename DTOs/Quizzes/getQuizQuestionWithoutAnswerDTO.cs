namespace Capstone.DTOs.Quizzes
{
    public class getQuizQuestionWithoutAnswerDTO
    {
        public int QuestionId { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public int Time { get; set; }
        public int Score { get; set; }
        public List<getQuizOptionWithoutAnswerDTO> Options { get; set; } = new();
    }
    public class getQuizOptionWithoutAnswerDTO
    {
        public int OptionId { get; set; }
        public string OptionContent { get; set; } = string.Empty;
    }
}

