using Capstone.DTOs.Reports.Teacher;
using Capstone.ENUMs;
using Capstone.Repositories.Histories;
using Microsoft.AspNetCore.Mvc;
using static Capstone.ENUMs.ExpiredEnumDTO;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherHistoryController : ControllerBase
    {
        private readonly ILogger<TeacherHistoryController> _logger;
        private readonly IHistoryTeacher _historyTeacherService;

        public TeacherHistoryController(ILogger<TeacherHistoryController> logger, IHistoryTeacher historyTeacherService)
        {
            _logger = logger;
            _historyTeacherService = historyTeacherService;
        }

        // ===== GET METHODS =====
        /// <summary>
        /// Get all delivered quizzes for a teacher
        /// </summary>
        [HttpGet("delivered-quizzes/{teacherId:int}")]
        public async Task<IActionResult> GetDeliveredQuizzes(int teacherId)
        {
            _logger.LogInformation("GetDeliveredQuizzes: Start - TeacherId={TeacherId}", teacherId);
            try
            {
                if (teacherId <= 0)
                {
                    _logger.LogWarning("GetDeliveredQuizzes: Invalid TeacherId={TeacherId}", teacherId);
                    return BadRequest(new { message = "Invalid teacher ID" });
                }

                var deliveredQuizzes = await _historyTeacherService.DeliveredQuizz(teacherId);
                _logger.LogInformation("GetDeliveredQuizzes: Retrieved {Count} quiz groups for TeacherId={TeacherId}", deliveredQuizzes?.Count ?? 0, teacherId);
                return Ok(deliveredQuizzes ?? new List<DeliveredQuizzDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDeliveredQuizzes: Error retrieving delivered quizzes for TeacherId={TeacherId}", teacherId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed report for a specific quiz in a group
        /// </summary>
        [HttpGet("report-detail/{groupId:int}/{quizId:int}")]
        public async Task<IActionResult> GetReportDetail(int groupId, int quizId)
        {
            _logger.LogInformation("GetReportDetail: Start - GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
            try
            {
                if (groupId <= 0 || quizId <= 0)
                {
                    _logger.LogWarning("GetReportDetail: Invalid parameters - GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
                    return BadRequest(new { message = "Invalid group ID or quiz ID" });
                }

                var reportDetail = await _historyTeacherService.ReportDetail(groupId, quizId);
                if (reportDetail == null)
                {
                    _logger.LogWarning("GetReportDetail: Report not found - GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
                    return NotFound(new { message = "Report not found" });
                }

                _logger.LogInformation("GetReportDetail: Success - GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
                return Ok(reportDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetReportDetail: Error retrieving report detail for GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get offline quiz results for students
        /// </summary>
        [HttpGet("offline-results/{quizId:int}")]
        public async Task<IActionResult> GetOfflineResult(int quizId)
        {
            _logger.LogInformation("GetOfflineResult: Start - QuizId={QuizId}", quizId);
            try
            {
                if (quizId <= 0)
                {
                    _logger.LogWarning("GetOfflineResult: Invalid QuizId={QuizId}", quizId);
                    return BadRequest(new { message = "Invalid quiz ID" });
                }

                var offlineResults = await _historyTeacherService.GetOfflineResult(quizId);
                _logger.LogInformation("GetOfflineResult: Retrieved {Count} offline results for QuizId={QuizId}", offlineResults?.Count ?? 0, quizId);
                return Ok(offlineResults ?? new List<ViewStudentHistoryDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOfflineResult: Error retrieving offline results for QuizId={QuizId}", quizId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get question history statistics for a quiz
        /// </summary>
        [HttpGet("question-history/{quizId:int}")]
        public async Task<IActionResult> ViewQuestionHistory(int quizId)
        {
            _logger.LogInformation("ViewQuestionHistory: Start - QuizId={QuizId}", quizId);
            try
            {
                if (quizId <= 0)
                {
                    _logger.LogWarning("ViewQuestionHistory: Invalid QuizId={QuizId}", quizId);
                    return BadRequest(new { message = "Invalid quiz ID" });
                }

                var questionHistory = await _historyTeacherService.ViewQuestionHistory(quizId);
                _logger.LogInformation("ViewQuestionHistory: Retrieved {Count} question statistics for QuizId={QuizId}", questionHistory?.Count ?? 0, quizId);
                return Ok(questionHistory ?? new List<ViewQuestionHistoryDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewQuestionHistory: Error retrieving question history for QuizId={QuizId}", quizId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed information about a specific question
        /// </summary>
        [HttpGet("question-detail/{questionId:int}")]
        public async Task<IActionResult> ViewDetailOfQuestion(int questionId)
        {
            _logger.LogInformation("ViewDetailOfQuestion: Start - QuestionId={QuestionId}", questionId);
            try
            {
                if (questionId <= 0)
                {
                    _logger.LogWarning("ViewDetailOfQuestion: Invalid QuestionId={QuestionId}", questionId);
                    return BadRequest(new { message = "Invalid question ID" });
                }

                var questionDetail = await _historyTeacherService.ViewDetailOfQuestion(questionId);
                if (questionDetail == null)
                {
                    _logger.LogWarning("ViewDetailOfQuestion: Question not found - QuestionId={QuestionId}", questionId);
                    return NotFound(new { message = "Question not found" });
                }

                _logger.LogInformation("ViewDetailOfQuestion: Success - QuestionId={QuestionId}", questionId);
                return Ok(questionDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewDetailOfQuestion: Error retrieving question detail for QuestionId={QuestionId}", questionId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ===== POST METHODS =====
        /// <summary>
        /// Check if a quiz has expired and update status if needed
        /// </summary>
        [HttpPost("check-expired")]
        public async Task<IActionResult> CheckExpiredTime([FromBody] CheckExpiredTimeRequest request)
        {
            _logger.LogInformation("CheckExpiredTime: Start - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("CheckExpiredTime: Request body null");
                    return BadRequest(new { message = "Request body is required" });
                }

                if (request.QuizId <= 0 || request.GroupId <= 0)
                {
                    _logger.LogWarning("CheckExpiredTime: Invalid parameters - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                    return BadRequest(new { message = "Invalid quiz ID or group ID" });
                }

                var isExpired = await _historyTeacherService.checkExpiredTime(request.QuizId, request.GroupId);
                _logger.LogInformation("CheckExpiredTime: Quiz expired status={IsExpired} for QuizId={QuizId}, GroupId={GroupId}", isExpired, request.QuizId, request.GroupId);
                return Ok(new { isExpired, message = isExpired ? "Quiz has expired and status updated" : "Quiz is still active" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckExpiredTime: Error checking expired time for QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// End a quiz immediately by setting status to Completed
        /// </summary>
        [HttpPost("end-now")]
        public async Task<IActionResult> EndNow([FromBody] EndNowRequest request)
        {
            _logger.LogInformation("EndNow: Start - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("EndNow: Request body null");
                    return BadRequest(new { message = "Request body is required" });
                }

                if (request.QuizId <= 0 || request.GroupId <= 0)
                {
                    _logger.LogWarning("EndNow: Invalid parameters - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                    return BadRequest(new { message = "Invalid quiz ID or group ID" });
                }

                var success = await _historyTeacherService.EndNow(request.GroupId, request.QuizId);
                if (success)
                {
                    _logger.LogInformation("EndNow: Success - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                    return Ok(new { message = "Quiz ended successfully" });
                }
                else
                {
                    _logger.LogWarning("EndNow: Failed to end quiz - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                    return BadRequest(new { message = "Failed to end quiz" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EndNow: Error ending quiz - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ===== PUT METHODS =====
        /// <summary>
        /// Change the expired time for a quiz in a group
        /// </summary>
        [HttpPut("change-expired-time")]
        public async Task<IActionResult> ChangeExpiredTime([FromBody] ChangeExpiredTimeRequest request)
        {
            _logger.LogInformation("ChangeExpiredTime: Start - QuizId={QuizId}, GroupId={GroupId}, NewExpiredTime={NewExpiredTime}", 
                request?.QuizId, request?.GroupId, request?.NewExpiredTime);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("ChangeExpiredTime: Request body null");
                    return BadRequest(new { message = "Request body is required" });
                }

                if (request.QuizId <= 0 || request.GroupId <= 0)
                {
                    _logger.LogWarning("ChangeExpiredTime: Invalid IDs - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                    return BadRequest(new { message = "Invalid quiz ID or group ID" });
                }

                var result = await _historyTeacherService.ChangeExpiredTime(request.GroupId, request.QuizId, request.NewExpiredTime);
                
                switch (result)
                {
                    case ExpiredEnum.Success:
                        _logger.LogInformation("ChangeExpiredTime: Success - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                        return Ok(new { message = "Expired time updated successfully" });
                    
                    case ExpiredEnum.QuizGroupNotFound:
                        _logger.LogWarning("ChangeExpiredTime: Quiz group not found - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                        return NotFound(new { message = "Quiz group not found" });
                    
                    case ExpiredEnum.InvalidExpiredTime:
                        _logger.LogWarning("ChangeExpiredTime: Invalid expired time - NewExpiredTime={NewExpiredTime}", request.NewExpiredTime);
                        return BadRequest(new { message = "Expired time must be in the future" });
                    
                    case ExpiredEnum.UpdateFailed:
                        _logger.LogWarning("ChangeExpiredTime: Update failed - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                        return BadRequest(new { message = "Failed to update expired time" });
                    
                    case ExpiredEnum.Error:
                    default:
                        _logger.LogError("ChangeExpiredTime: Unknown error - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                        return StatusCode(500, new { message = "Internal server error" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeExpiredTime: Unexpected error - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Change the name of a report
        /// </summary>
        [HttpPut("change-report-name")]
        public async Task<IActionResult> ChangeReportName([FromBody] ChangeReportNameRequest request)
        {
            _logger.LogInformation("ChangeReportName: Start - ReportId={ReportId}, NewName={NewName}", request?.ReportId, request?.NewReportName);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("ChangeReportName: Request body null");
                    return BadRequest(new { message = "Request body is required" });
                }

                if (request.ReportId <= 0)
                {
                    _logger.LogWarning("ChangeReportName: Invalid ReportId={ReportId}", request.ReportId);
                    return BadRequest(new { message = "Invalid report ID" });
                }

                if (string.IsNullOrWhiteSpace(request.NewReportName))
                {
                    _logger.LogWarning("ChangeReportName: Empty report name for ReportId={ReportId}", request.ReportId);
                    return BadRequest(new { message = "Report name cannot be empty" });
                }

                var success = await _historyTeacherService.ChangeReportName(request.ReportId, request.NewReportName);
                if (success)
                {
                    _logger.LogInformation("ChangeReportName: Success - ReportId={ReportId}, NewName={NewName}", request.ReportId, request.NewReportName);
                    return Ok(new { message = "Report name updated successfully" });
                }
                else
                {
                    _logger.LogWarning("ChangeReportName: Failed to update - ReportId={ReportId}", request.ReportId);
                    return NotFound(new { message = "Report not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeReportName: Error updating report name - ReportId={ReportId}", request?.ReportId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    // Request DTOs for the controller
}
