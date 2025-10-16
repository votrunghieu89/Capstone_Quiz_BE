﻿using Capstone.Database;
using Capstone.DTOs;
using Capstone.DTOs.Quizzes;
using Capstone.Model;
using Capstone.Repositories.Quizzes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly ILogger<QuizController> _logger;
        private readonly IQuizRepository _quizRepository;
        private readonly Redis _redis;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public QuizController(ILogger<QuizController> logger, IQuizRepository quizRepository, Redis redis, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _quizRepository = quizRepository;
            _redis = redis;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        // ===== GET METHODS =====
        [HttpGet("GetQuestionOfQuizCache/{quizId}")]
        public async Task<IActionResult> GetQuizById(int quizId)
        {
            var json = await _redis.GetStringAsync($"quiz_questions_{quizId}"); // lấy câu hỏi từ Redis
            if (json == null)
            {
                var quiz = await _quizRepository.GetAllQuestionEachQuiz(quizId); // lấy câu hỏi từ database
                if (quiz == null)
                {
                    return NotFound(new { message = "Quiz not found." });
                }
                return Ok(quiz);
            }
            else
            {
                var questions = JsonSerializer.Deserialize<List<GetQuizQuestionsDTO>>(json); // chuyển json thành object
                if (questions == null)
                {
                    _logger.LogError("Failed to deserialize cached questions for quizId: {QuizId}", quizId);
                    return StatusCode(500, "An error occurred while processing the cached data.");
                }
                return Ok(questions);
            }
        }

        [HttpGet("getDetailOfAQuiz/{quizId}")]
        public async Task<IActionResult> getDetailOfAQuiz(int quizId)
        {
            ViewDetailDTO quizDetails = await _quizRepository.getDetailOfAQuiz(quizId);
            if (quizDetails == null)
            {
                return NotFound(new { message = "Quiz not found." });
            }
            if (!string.IsNullOrEmpty(quizDetails.AvatarURL))
            {
                quizDetails.AvatarURL = $"{Request.Scheme}://{Request.Host}/{quizDetails.AvatarURL.Replace("\\", "/")}";
            }
            ViewDetailDTO quiz = new ViewDetailDTO
            {
                QuizId = quizDetails.QuizId,

                Title = quizDetails.Title,
                Description = quizDetails.Description,
                AvatarURL = quizDetails.AvatarURL ?? string.Empty,
                TotalParticipants = quizDetails.TotalParticipants,
                TotalQuestions = quizDetails.TotalQuestions,
                CreatedDate = quizDetails.CreatedDate,
                Questions = quizDetails.Questions,

            };
            return Ok(quiz);
        }

        [HttpGet("GetAllQuizzes")]
        public async Task<IActionResult> GetAllQuizzes([FromQuery] PaginationDTO pages)
        {
            if (pages.page <= 0 || pages.pageSize <= 0)
                return BadRequest(new { message = "Page and PageSize must be greater than 0." });

            string cacheKey = $"all_quizzes_page_{pages.page}_size_{pages.pageSize}";
            // all_quizzes_page_{page}_size_{pageSize}
            var cachedJson = await _redis.GetStringAsync(cacheKey);
            if (cachedJson == null)
            {

            }
            List<ViewAllQuizDTO>? quizzes = null;

            if (!string.IsNullOrEmpty(cachedJson))
            {
                quizzes = JsonSerializer.Deserialize<List<ViewAllQuizDTO>>(cachedJson);

            }


            if (quizzes == null || !quizzes.Any())
            {
                quizzes = await _quizRepository.getAllQuizzes(pages.page, pages.pageSize);

                if (quizzes == null || !quizzes.Any())
                    return NotFound(new { message = "No quizzes found." });
            }
            foreach (var quiz in quizzes)
            {
                if (!string.IsNullOrEmpty(quiz.AvatarURL))
                {
                    quiz.AvatarURL = $"{Request.Scheme}://{Request.Host}/{quiz.AvatarURL.Replace("\\", "/")}";
                }
            }

            return Ok(quizzes);
        }

        // ===== POST METHODS =====
        [HttpPost("uploadImage")]
        public async Task<IActionResult> UploadImage([FromForm] QuizCreateFormDTO dto)
        {
            var folderName = _configuration["UploadSettings:QuizFolder"];
            var uploadFolder = Path.Combine(_webHostEnvironment.ContentRootPath, folderName);

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            string avatarPath = Path.Combine(folderName, "Default.jpg");

            if (dto.AvatarURL != null && dto.AvatarURL.Length <= 2 * 1024 * 1024) // 2MB
            {
                var extension = Path.GetExtension(dto.AvatarURL.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                await dto.AvatarURL.CopyToAsync(fileStream);

                avatarPath = Path.Combine(folderName, uniqueFileName);
            }

            return Ok(new { imageUrl = avatarPath.Replace("\\", "/") });
        }

        [HttpPost("createQuiz")]
        public async Task<IActionResult> CreateQuiz([FromBody] QuizCreateDTo quiz)
        {
            Console.WriteLine("Received quiz data: " + quiz.AvatarURL);
            try
            {
                var quizModel = new QuizCreateDTo
                {
                    TeacherId = quiz.TeacherId,
                    FolderId = quiz.FolderId,
                    TopicId = quiz.TopicId,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    IsPrivate = quiz.IsPrivate,
                    AvatarURL = quiz.AvatarURL,
                    NumberOfPlays = 0,
                    CreatedAt = DateTime.Now,
                    Questions = quiz.Questions?.Select(q => new QuestionDTO
                    {
                        QuestionType = q.QuestionType,
                        QuestionContent = q.QuestionContent,
                        Time = q.Time,
                        Options = q.Options?.Select(o => new OptionDTO
                        {
                            OptionContent = o.OptionContent,
                            IsCorrect = o.IsCorrect
                        }).ToList() ?? new List<OptionDTO>()
                    }).ToList() ?? new List<QuestionDTO>()
                };
                Console.WriteLine(quizModel.AvatarURL);
                // 5️⃣ Gọi repository lưu quiz
                var createdQuiz = await _quizRepository.CreateQuiz(quizModel);
                if (createdQuiz == null)
                    return StatusCode(500, "An error occurred while creating the quiz.");

                return Ok(createdQuiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quiz");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost("CheckQuizAnswers")]
        public async Task<IActionResult> CheckQuizAnswers([FromBody] CheckAnswerDTO quizAnswers)
        {
            var result = await _quizRepository.checkAnswer(quizAnswers);
            if(quizAnswers == null || result == false)
            {
                return BadRequest(new { message = "Invalid quiz answers provided." });
            }
            return Ok(new { message = "Answers checked successfully.", isAllCorrect = result });
        }

        [HttpPost("GetCorrectAnswers")]
        public async Task<IActionResult> GetCorrectAnswers([FromBody] GetCorrectAnswer getCorrectAnswer)
        {
            var correctAnswers = await _quizRepository.getCorrectAnswer(getCorrectAnswer);
            if (correctAnswers == null)
            {
                return NotFound(new { message = "No correct answers found for the provided question IDs." });
            }
            return Ok(correctAnswers);
        }

        // ===== PUT METHODS =====
        [HttpPut("updateImage")]
        public async Task<IActionResult> UpdateImage([FromForm] QuizUpdateImageDTO dto)
        {
            try
            {
                var oldImage = await _quizRepository.getOrlAvatarURL(dto.QuizId);

                // Nếu user không upload ảnh mới -> giữ nguyên ảnh cũ
                if (dto.AvatarURL == null)
                {
                    return Ok(new { imageUrl = oldImage });
                }

                // Validate dung lượng
                if (dto.AvatarURL.Length > 2 * 1024 * 1024) // 2MB
                {
                    return BadRequest("File quá lớn, tối đa 2MB");
                }

                // Validate extension
                var extension = Path.GetExtension(dto.AvatarURL.FileName).ToLower();
                var allowedExt = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowedExt.Contains(extension))
                {
                    return BadRequest("Định dạng file không hợp lệ (chỉ cho phép .jpg, .jpeg, .png)");
                }

                // Tạo folder
                var folderName = _configuration["UploadSettings:QuizFolder"];
                var uploadFolder = Path.Combine(_webHostEnvironment.ContentRootPath, folderName);
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // Tạo file mới
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    await dto.AvatarURL.CopyToAsync(stream);
                }

                // Sau khi lưu file mới thành công -> mới xóa file cũ
                if (!string.IsNullOrEmpty(oldImage) && oldImage != "Default.jpg")
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.ContentRootPath, oldImage);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                return Ok(new { imageUrl = Path.Combine(folderName, uniqueFileName).Replace("\\", "/") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPut("updateQuiz")]
        public async Task<IActionResult> UpdateQuiz([FromBody] QuizzUpdateControllerDTO quiz)
        {
            try
            {
                var quizModel = new QuizUpdateDTO
                {
                    QuizId = quiz.QuizId,
                    FolderId = quiz.FolderId,
                    TopicId = quiz.TopicId,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    IsPrivate = quiz.IsPrivate,
                    AvartarURL = quiz.AvartarURL,
                    Questions = quiz.Questions?.Select(q => new QuestionUpdateDTO
                    {
                        QuestionId = q.QuestionId,
                        QuestionType = q.QuestionType,
                        QuestionContent = q.QuestionContent,
                        Time = q.Time,
                        Options = q.Options?.Select(o => new OptionUpdateDTO
                        {
                            OptionId = o.OptionId,
                            OptionContent = o.OptionContent,
                            IsCorrect = o.IsCorrect
                        }).ToList() ?? new List<OptionUpdateDTO>()
                    }).ToList() ?? new List<QuestionUpdateDTO>()
                };
                var updatedQuiz = await _quizRepository.UpdateQuiz(quizModel);
                if (updatedQuiz == null)
                {
                    return StatusCode(500, "An error occurred while updating the quiz.");
                }
                // Xoá cache câu hỏi của quiz này trong Redis
                await _redis.DeleteKeysByPatternAsync($"quiz_questions_{quiz.QuizId}*");
                return Ok(updatedQuiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quiz with ID: {QuizId}", quiz.QuizId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // ===== DELETE METHODS =====
        [HttpDelete("deleteQuiz/{quizId}")]
        public async Task<IActionResult> DeleteQuiz(int quizId)
        {
            string isDeleted = await _quizRepository.DeleteQuiz(quizId);
            if (isDeleted == null)
            {
                return NotFound(new { message = "Quiz not found or could not be deleted." });
            }
            if(isDeleted == "QuizImage/Default.jpg")
            {
                return Ok(new { message = "Quiz deleted successfully." });
            }
            else
            {
                var imagePath = Path.Combine(_webHostEnvironment.ContentRootPath, isDeleted);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
                return Ok(new { message = "Quiz deleted successfully." });
            }
        }

        [HttpDelete("deleteQuestion/{questionId}")]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            bool isDeleted = await _quizRepository.DeleteQuestion(questionId);
            if (!isDeleted)
            {
                return NotFound(new { message = "Question not found or could not be deleted." });
            }
            return Ok(new { message = "Question deleted successfully." });
        }

        [HttpDelete("ClearQuizCache/{quizId}")]
        public async Task<IActionResult> ClearQuizCache(int quizId)
        {
            try
            {
                await _redis.DeleteKeysByPatternAsync($"quiz_questions_{quizId}*");
                return Ok(new { message = $"Cache for quiz {quizId} cleared successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache for quizId: {QuizId}", quizId);
                return StatusCode(500, "Error clearing quiz cache.");
            }
        }
    }
}
