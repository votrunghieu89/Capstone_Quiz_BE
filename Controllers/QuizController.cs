using Capstone.Database;
using Capstone.DTOs.Quizzes;
using Capstone.Model;
using Capstone.Repositories.Quizzes;
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
        public QuizController(ILogger<QuizController> logger, IQuizRepository quizRepository, Redis redis)
        {
            _logger = logger;
            _quizRepository = quizRepository;
            _redis = redis;
        }
        [HttpPost("createQuiz")]
        public async Task<IActionResult> CreateQuiz([FromBody] QuizCreateDTo quiz)
        {
            QuizModel quizModel = new QuizModel()
            {
                TeacherId = quiz.TeacherId,
                FolderId = quiz.FolderId,
                TopicId = quiz.TopicId,
                GroupId = quiz.GroupId,
                Title = quiz.Title,
                Description = quiz.Description,
                IsPrivate = quiz.IsPrivate,
                AvartarURL = quiz.AvartarURL,
                NumberOfPlays = 0,
                CreateAt = DateTime.Now,
                Questions = quiz.Questions.Select(q => new QuestionModel
                {
                    QuestionType = q.QuestionType,
                    QuestionContent = q.QuestionContent,
                    Time = q.Time,
                    Options = q.Options.Select(o => new OptionModel
                    {
                        OptionContent = o.OptionContent,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList()

            };
            var createdQuiz = await _quizRepository.CreateQuiz(quizModel);
            if (createdQuiz == null)
            {
                return StatusCode(500, "An error occurred while creating the quiz.");
            }
            return Ok(createdQuiz);
        }

        [HttpDelete("deleteQuiz/{quizId}")]
        public async Task<IActionResult> DeleteQuiz(int quizId)
        {
            bool isDeleted = await _quizRepository.DeleteQuiz(quizId);
            if (!isDeleted)
            {
                return NotFound(new { message = "Quiz not found or could not be deleted." });
            }
            return Ok(new { message = "Quiz deleted successfully." });
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
        [HttpGet("GetQuestionOfQuizC/{quizId}")]
        public async Task<IActionResult> GetQuizById(int quizId)
        {
            var json = await _redis.GetStringAsync($"quiz_questions_{quizId}"); // lấy câu hỏi từ Redis
            if (json == null)
            {
                var quiz = await _quizRepository.GetQuizQuestions(quizId); // lấy câu hỏi từ database
                if (quiz == null)
                {
                    return NotFound(new { message = "Quiz not found." });
                }
                return Ok(quiz);
            } else
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
        [HttpPost("CheckQuizAnswers")]
        public async Task<IActionResult> CheckQuizAnswers([FromBody] CheckAnswerDTO quizAnswers)
        {
            var result = await _quizRepository.checkAnswer(quizAnswers);
            if (result == null)
            {
                return StatusCode(500, "An error occurred while checking the answers.");
            }
            return Ok(result);
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
