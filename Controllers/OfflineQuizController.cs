using Capstone.DTOs;
using Capstone.Repositories.Quizzes;
using Capstone.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OfflineQuizController : ControllerBase
    {
        private IOfflineQuizRepository _offlineRepo;
        private readonly ILogger<OfflineQuizController> _logger;

        // Constructor
        public OfflineQuizController(IOfflineQuizRepository offlineQuizRepository, ILogger<OfflineQuizController> logger)
        {
            _logger = logger;
            _offlineRepo = offlineQuizRepository;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartQuiz([FromBody] StartOfflineQuizDTO dto)
        {
            try
            {
                await _offlineRepo.StartOfflineQuiz(dto);
                return Ok(new { message = "Quiz started successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting offline quiz.");
                return BadRequest(new { message = ex.Message });
            }
        }

        // Đây là endpoint để học sinh gửi đáp án của mỗi câu hỏi.
        [HttpPost("answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] StudentAnswerSubmissionDTO dto)
        {
            try
            {
                // Gọi service để xử lý đáp án và cập nhật Redis Cache
                var result = await _offlineRepo.ProcessStudentAnswer(dto);

                if (!result)
                {
                    return BadRequest(new { message = "Failed to process answer or session expired." });
                }

                return Ok(new { message = "Answer saved to session successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing student answer.");
                // Trả về 500 nếu lỗi server hoặc 400 nếu lỗi dữ liệu/session
                return StatusCode(500, new { message = $"Error processing answer: {ex.Message}" });
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuiz([FromBody] FinishOfflineQuizDTO dto)
        {
            try
            {
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _offlineRepo.SubmitOfflineQuiz(dto,accountId,ipAddess);

                if (result == null)
                {
                    return BadRequest(new { message = "Failed to submit quiz. Check MaxAttempts or session integrity." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting offline quiz.");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("result/{studentId}/{quizId}")]
        public async Task<IActionResult> GetResult(int studentId, int quizId)
        {
            try
            {
                // Lấy kết quả cuối cùng từ DB (bao gồm cả Rank)
                var result = await _offlineRepo.GetOfflineResult(studentId, quizId);

                if (result == null)
                    return NotFound(new { message = "No results found." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offline quiz result.");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}