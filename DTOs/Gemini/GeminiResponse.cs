using Capstone.DTOs.Quizzes;

namespace Capstone.DTOs.Gemini
{
    public class GeminiResponse
    {
        public List<QuestionResponse> questionResponses { get; set; } = new List<QuestionResponse>();
    }
    public class QuestionResponse
    {
        public string QuestionContent { get; set; } = string.Empty;
        public List<OptionDTO> Options { get; set; } = new();
    }
}

