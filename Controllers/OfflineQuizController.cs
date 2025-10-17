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

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuiz([FromBody] FinishOfflineQuizDTO dto)
        {
            try
            {
                var result = await _offlineRepo.SubmitOfflineQuiz(dto);
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

