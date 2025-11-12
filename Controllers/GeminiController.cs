using Capstone.DTOs.Gemini;
using Capstone.DTOs.Quizzes;
using Capstone.ENUMs;
using Capstone.Repositories;
using Capstone.Repositories.Quizzes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public GeminiController(ILogger<GeminiController> logger, IGemeniService geminiService, IQuizRepository quizRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _geminiService = geminiService;
            _quizRepository = quizRepository;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("CreateQuizByGemini")]
        [Authorize(Roles = "Teacher")]
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
                    return BadRequest(new { message = "File type is not supported." });
                case ConvertTextEnum.ComvertText.ERROR:
                    return BadRequest(new { message = "Error while processing the file." });
                case ConvertTextEnum.ComvertText.NULL:
                    return BadRequest(new { message = "Unable to read content from file." });
                case ConvertTextEnum.ComvertText.PDF:
                    using (var stream = input.formFile.OpenReadStream())
                        text = await _geminiService.ExtractTextFromPdf(stream);
                    break;
                case ConvertTextEnum.ComvertText.DOCX:
                    using (var stream = input.formFile.OpenReadStream())
                        text = await _geminiService.ExtractTextFromWord(stream);
                    break;
                default:
                    return BadRequest(new { message = "Unsupported file format." });
            }
            var prompt = $@"
                            INPUT IS TEXT (EXTRACTED FROM DOCX OR PDF FILE):
                            '{text}'

                            REQUIREMENTS:
                            1. **Extract Existing Questions:** If the input text already contains numbered questions with options A, B, C, D, extract the questions and options **exactly**.
                            2. **Identify Correct Answers:** If an option is marked with an asterisk `*` before/after it, or contains the keyword ""CORRECT ANSWER"", treat it as the correct answer.
                            3. **Generate Additional Questions (If Needed):** If the number of extracted questions is less than {input.NumberOfQuestion}, generate additional questions from the remaining content to reach a total of {input.NumberOfQuestion} questions. Each newly generated question must have 4 options and **must not duplicate any existing question content**.
                            4. **Output Format:** Return **ONLY** a valid JSON object, **no explanations, comments, or markdown**.

                            REQUIRED JSON FORMAT:
                            {{
                              ""questions"": [
                                {{
                                  ""questionContent"": ""string (question content)"",
                                  ""options"": [
                                    {{ ""optionContent"": ""string (option 1)"", ""isCorrect"": true/false }},
                                    {{ ""optionContent"": ""string (option 2)"", ""isCorrect"": true/false }},
                                    {{ ""optionContent"": ""string (option 3)"", ""isCorrect"": true/false }},
                                    {{ ""optionContent"": ""string (option 4)"", ""isCorrect"": true/false }}
                                  ]
                                }}
                              ]
                            }}

                            DETAILED INSTRUCTIONS:
                            - Keep existing correct answers as-is.
                            - When generating new questions, ensure content does **not duplicate any existing questions**.
                            - Return exactly {input.NumberOfQuestion} questions.
                            - Do not add explanations, commentary, or markdown formatting; only return valid JSON.
                            ";

            var jsonResponse = await _geminiService.GenerateQuestions(prompt);
            if (jsonResponse == null)
                return BadRequest(new { message = "Failed to generate questions using Gemini API." });
            List<QuestionDTO> questionDTOs = await _geminiService.ConvertJsonToQuestion(jsonResponse, input.Time, input.Score);
            if (questionDTOs == null || !questionDTOs.Any())
                return BadRequest(new { message = "No questions were generated from the Gemini response." });
            // xử lí ảnh
            var folderName = _configuration["UploadSettings:QuizFolder"];
            var uploadFolder = Path.Combine(_webHostEnvironment.ContentRootPath, folderName);

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            string avatarPath = Path.Combine(folderName, "Default.jpg");

            if (input.AvatarURL != null && input.AvatarURL.Length <= 2 * 1024 * 1024) // 2MB
            {
                var extension = Path.GetExtension(input.AvatarURL.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                await input.AvatarURL.CopyToAsync(fileStream);

                avatarPath = Path.Combine(folderName, uniqueFileName);
            }
            var quizModel = new QuizCreateDTo
            {
                TeacherId = input.TeacherId,
                FolderId = input.FolderId,
                TopicId = input.TopicId,
                Title = input.Title,
                Description = input.Description,
                IsPrivate = input.IsPrivate,
                AvatarURL = avatarPath,
                NumberOfPlays = 0,
                CreatedAt = DateTime.Now,
                Questions = questionDTOs,
            };
            var createdQuiz = await _quizRepository.CreateQuiz(quizModel, ipAddess);
            if (createdQuiz == null)
            {
                return StatusCode(500, "Đã xảy ra lỗi khi tạo bài kiểm tra.");
            }
            return Ok(new
            {
                message = "Quiz created successfully.",
                quiz = createdQuiz
            });
        }
        [HttpPost("convert-json-to-questions")]
        public async Task<IActionResult> ConvertJsonToQuestion([FromBody] ConvertQuestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Json))
            {
                _logger.LogWarning("ConvertJsonToQuestion: JSON is null or empty");
                return BadRequest(new { message = "JSON input cannot be empty." });
            }

            var result = await _geminiService.ConvertJsonToQuestion(request.Json, request.Time, request.Score);

            if (result == null || result.Count == 0)
            {
                _logger.LogWarning("ConvertJsonToQuestion: No valid questions parsed from JSON");
                return BadRequest(new { message = "Không thể parse được câu hỏi từ JSON" });
            }

            _logger.LogInformation("ConvertJsonToQuestion: Successfully parsed {Count} questions", result.Count);
            return Ok(result);
        }
    }

    // ✅ Model request để bind dữ liệu đầu vào
    public class ConvertQuestionRequest
    {
        public string Json { get; set; }
        public int Time { get; set; } = 30;
        public int Score { get; set; } = 10;
    }
}
