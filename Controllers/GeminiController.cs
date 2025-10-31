using Capstone.DTOs.Gemini;
using Capstone.DTOs.Quizzes;
using Capstone.ENUMs;
using Capstone.Repositories;
using Capstone.Repositories.Quizzes;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly ILogger<GeminiController> _logger;
        private readonly IGemeniService _geminiService;
        private readonly IQuizRepository _quizRepository;

        public GeminiController(ILogger<GeminiController> logger, IGemeniService geminiService, IQuizRepository quizRepository)
        {
            _logger = logger;
            _geminiService = geminiService;
            _quizRepository = quizRepository;
        }

        [HttpPost("CreateQuizByGemini")]
        public async Task<IActionResult> CreateQuizByGemini([FromForm] Gemini_InputDTO input)
        {
            var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            if (input == null)
            {
                return BadRequest();
            }
            string text;
            var TypeOfFile = _geminiService.ConvertToText(input.formFile);
            switch (TypeOfFile)
            {
                case ConvertTextEnum.ComvertText.NOTSUPPORT:
                    return BadRequest();
                case ConvertTextEnum.ComvertText.ERROR:
                    return BadRequest();
                case ConvertTextEnum.ComvertText.NULL:
                    return BadRequest();
                case ConvertTextEnum.ComvertText.PDF:
                    using (var stream = input.formFile.OpenReadStream())
                    {
                        text = await _geminiService.ExtractTextFromPdf(stream);
                    }
                    break;
                case ConvertTextEnum.ComvertText.DOCX:
                    using (var stream = input.formFile.OpenReadStream())
                    {
                        text = await _geminiService.ExtractTextFromWord(stream);
                    }
                    break;
                default:
                    return BadRequest();
            }
            var prompt = $@"
                                Tạo ra {input.NumberOfQuestion} câu hỏi trắc nghiệm với 4 câu trả lời từ nội dung sau:
                                '{text}'

                                Yêu cầu trả về đúng định dạng JSON sau, không thêm mô tả, không có ký tự dư:
                                {{
                                  ""questions"": [
                                    {{
                                      ""questionContent"": ""string"",
                                      ""options"": [
                                        {{
                                          ""optionContent"": ""string"",
                                          ""isCorrect"": true
                                        }}
                                      ]
                                    }}
                                  ]
                                }}";
            var jsonResponse = await _geminiService.GenerateQuestions(prompt);
            List<QuestionDTO> questionDTOs = await _geminiService.ConvertJsonToQuestion(jsonResponse, input.Time, input.Score);

            var quizModel = new QuizCreateDTo
            {
                TeacherId = input.TeacherId,
                FolderId = input.FolderId,
                TopicId = input.TopicId,
                Title = input.Title,
                Description = input.Description,
                IsPrivate = input.IsPrivate,
                AvatarURL = input.AvatarURL,
                NumberOfPlays = 0,
                CreatedAt = DateTime.Now,
                Questions = questionDTOs,
            };
            var createdQuiz = await _quizRepository.CreateQuiz(quizModel, ipAddess);
            if (createdQuiz == null)
            {
                return StatusCode(500, "An error occurred while creating the quiz.");
            }
            return Ok(createdQuiz);
        }
    }
}
