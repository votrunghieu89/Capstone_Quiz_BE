using Capstone.DTOs.Quizzes;
using System.Text.Json.Serialization;

namespace Capstone.DTOs.Gemini
{
    public class GeminiResponse
    {
        [JsonPropertyName("questions")]
        public List<QuestionResponse> questionResponses { get; set; } = new List<QuestionResponse>();
    }
    public class QuestionResponse
    {
        [JsonPropertyName("questionContent")]
        public string questionContent { get; set; } = string.Empty;
        [JsonPropertyName("options")]
        public List<OptionDTO> options { get; set; } = new();
    }
}

