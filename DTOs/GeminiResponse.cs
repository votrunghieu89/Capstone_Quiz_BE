namespace Capstone.DTOs
{
    public class GeminiResponse
    {
        List<QuestionResponse> questionResponses { get; set; } = new List<QuestionResponse>();
    }
    public class QuestionResponse
    {
        public string QuestionContent { get; set; } = string.Empty;
        public List<OptionResponse> Options { get; set; } = new();
    }
    public class OptionResponse
    {
        public string OptionContent { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }

    }
}

