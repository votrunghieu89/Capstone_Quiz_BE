using Capstone.DTOs.Reports.Teacher;
using Capstone.DTOs.Reports.Teacher.OfflineReport;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
using Capstone.ENUMs;
using Capstone.Repositories.Histories;
using Microsoft.AspNetCore.Mvc;
using static Capstone.ENUMs.ExpiredEnumDTO;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherReportController : ControllerBase
    {
        private readonly ILogger<TeacherReportController> _logger;
        private readonly ITeacherReportRepository _historyTeacherService;

        public TeacherReportController(ILogger<TeacherReportController> logger, ITeacherReportRepository historyTeacherService)
        {
            _logger = logger;
            _historyTeacherService = historyTeacherService;
        }

        // ===== GET METHODS =====

        /// <summary>
        /// Get all offline quiz reports for a teacher
        /// </summary>
        [HttpGet("offline/quiz-reports/{teacherId:int}")]
        public async Task<IActionResult> GetOfflineQuizReports(int teacherId)
        {
            _logger.LogInformation("GetOfflineQuizReports: Start - TeacherId={TeacherId}", teacherId);
            try
            {
                if (teacherId <= 0)
                {
                    _logger.LogWarning("GetOfflineQuizReports: Invalid TeacherId={TeacherId}", teacherId);
                    return BadRequest(new { message = "Invalid teacher ID" });
                }

                var offlineReports = await _historyTeacherService.GetOfflineQuizz(teacherId);
                _logger.LogInformation("GetOfflineQuizReports: Retrieved {Count} offline reports for TeacherId={TeacherId}", offlineReports?.Count ?? 0, teacherId);
                return Ok(offlineReports ?? new List<ViewAllOfflineReportDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOfflineQuizReports: Error retrieving offline reports for TeacherId={TeacherId}", teacherId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed offline report for a specific quiz
        /// </summary>
        [HttpGet("offline/detail-report")]
        public async Task<IActionResult> GetOfflineDetailReport([FromQuery] OfflineDetailReportRequest request)
        {
            _logger.LogInformation("GetOfflineDetailReport: Start - OfflineReportId={OfflineReportId}, QuizId={QuizId}",
                request?.OfflineReportId, request?.QuizId);
            try
            {
                if (request == null || request.OfflineReportId <= 0 || request.QuizId <= 0)
                {
                    _logger.LogWarning("GetOfflineDetailReport: Invalid parameters - OfflineReportId={OfflineReportId}, QuizId={QuizId}",
                        request?.OfflineReportId, request?.QuizId);
                    return BadRequest(new { message = "Invalid offline report ID or quiz ID" });
                }

                var detailReport = await _historyTeacherService.OfflineDetailReportEachQuiz(request.OfflineReportId, request.QuizId);
                if (detailReport == null)
                {
                    _logger.LogWarning("GetOfflineDetailReport: Report not found - OfflineReportId={OfflineReportId}, QuizId={QuizId}",
                        request.OfflineReportId, request.QuizId);
                    return NotFound(new { message = "Offline report not found" });
                }

                _logger.LogInformation("GetOfflineDetailReport: Success - OfflineReportId={OfflineReportId}, QuizId={QuizId}",
                    request.OfflineReportId, request.QuizId);
                return Ok(detailReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOfflineDetailReport: Error retrieving detail report - OfflineReportId={OfflineReportId}, QuizId={QuizId}",
                    request?.OfflineReportId, request?.QuizId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get offline student report for a specific quiz
        /// </summary>
        [HttpGet("offline/student-report")]
        public async Task<IActionResult> GetOfflineStudentReport([FromQuery] OfflineStudentReportRequest request)
        {
            _logger.LogInformation("GetOfflineStudentReport: Start - QuizId={QuizId}, QGId={GroupId}",
                request?.QuizId, request?.QGId, request?.GroupId);
            try
            {
                if (request == null || request.QuizId <= 0 || request.QGId <= 0 || request.GroupId <= 0)
                {
                    _logger.LogWarning("GetOfflineStudentReport: Invalid parameters - QuizId={QuizId}, QGId={GroupId}",
                        request?.QuizId, request?.QGId, request?.GroupId);
                    return BadRequest(new { message = "Invalid quiz ID, QG ID, or group ID" });
                }

                var studentReport = await _historyTeacherService.OfflineStudentReportEachQuiz(request.QuizId, request.QGId, request.GroupId);
                _logger.LogInformation("GetOfflineStudentReport: Retrieved {Count} student records - QuizId={QuizId}",
                    studentReport?.Count ?? 0, request.QuizId);
                return Ok(studentReport ?? new List<ViewOfflineStudentReportEachQuizDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOfflineStudentReport: Error retrieving student report - QuizId={QuizId}, QGId={GroupId}",
                    request?.QuizId, request?.QGId, request?.GroupId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get offline question report for a specific quiz
        /// </summary>
        [HttpGet("offline/question-report")]
        public async Task<IActionResult> GetOfflineQuestionReport([FromQuery] OfflineQuestionReportRequest request)
        {
            _logger.LogInformation("GetOfflineQuestionReport: Start - QuizId={QuizId}, QGId={GroupId}",
                request?.QuizId, request?.QGId, request?.GroupId);
            try
            {
                if (request == null || request.QuizId <= 0 || request.QGId <= 0 || request.GroupId <= 0)
                {
                    _logger.LogWarning("GetOfflineQuestionReport: Invalid parameters - QuizId={QuizId}, QGId={GroupId}",
                        request?.QuizId, request?.QGId, request?.GroupId);
                    return BadRequest(new { message = "Invalid quiz ID, QG ID, or group ID" });
                }

                var questionReport = await _historyTeacherService.OfflineQuestionReportEachQuiz(request.QuizId, request.QGId, request.GroupId);
                _logger.LogInformation("GetOfflineQuestionReport: Retrieved {Count} question statistics - QuizId={QuizId}",
                    questionReport?.Count ?? 0, request.QuizId);
                return Ok(questionReport ?? new List<ViewOfflineQuestionReportEachQuizDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOfflineQuestionReport: Error retrieving question report - QuizId={QuizId}, QGId={GroupId}",
                    request?.QuizId, request?.QGId, request?.GroupId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all online quiz reports for a teacher
        /// </summary>
        [HttpGet("online/quiz-reports/{teacherId:int}")]
        public async Task<IActionResult> GetOnlineQuizReports(int teacherId)
        {
            _logger.LogInformation("GetOnlineQuizReports: Start - TeacherId={TeacherId}", teacherId);
            try
            {
                if (teacherId <= 0)
                {
                    _logger.LogWarning("GetOnlineQuizReports: Invalid TeacherId={TeacherId}", teacherId);
                    return BadRequest(new { message = "Invalid teacher ID" });
                }

                var onlineReports = await _historyTeacherService.GetOnlineQuiz(teacherId);
                _logger.LogInformation("GetOnlineQuizReports: Retrieved {Count} online reports for TeacherId={TeacherId}", onlineReports?.Count ?? 0, teacherId);
                return Ok(onlineReports ?? new List<ViewAllOnlineReportDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOnlineQuizReports: Error retrieving online reports for TeacherId={TeacherId}", teacherId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed online report for a specific quiz
        /// </summary>
        [HttpGet("online/detail-report")]
        public async Task<IActionResult> GetOnlineDetailReport([FromQuery] OnlineDetailReportRequest request)
        {
            _logger.LogInformation("GetOnlineDetailReport: Start - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                request?.QuizId, request?.OnlineReportId);
            try
            {
                if (request == null || request.QuizId <= 0 || request.OnlineReportId <= 0)
                {
                    _logger.LogWarning("GetOnlineDetailReport: Invalid parameters - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                        request?.QuizId, request?.OnlineReportId);
                    return BadRequest(new { message = "Invalid quiz ID or online report ID" });
                }

                var detailReport = await _historyTeacherService.OnlineDetailReportEachQuiz(request.QuizId, request.OnlineReportId);
                if (detailReport == null)
                {
                    _logger.LogWarning("GetOnlineDetailReport: Report not found - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                        request.QuizId, request.OnlineReportId);
                    return NotFound(new { message = "Online report not found" });
                }

                _logger.LogInformation("GetOnlineDetailReport: Success - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                    request.QuizId, request.OnlineReportId);
                return Ok(detailReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOnlineDetailReport: Error retrieving detail report - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                    request?.QuizId, request?.OnlineReportId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get online student report for a specific quiz
        /// </summary>
        [HttpGet("online/student-report")]
        public async Task<IActionResult> GetOnlineStudentReport([FromQuery] OnlineStudentReportRequest request)
        {
            _logger.LogInformation("GetOnlineStudentReport: Start - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                request?.QuizId, request?.OnlineReportId);
            try
            {
                if (request == null || request.QuizId <= 0 || request.OnlineReportId <= 0)
                {
                    _logger.LogWarning("GetOnlineStudentReport: Invalid parameters - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                        request?.QuizId, request?.OnlineReportId);
                    return BadRequest(new { message = "Invalid quiz ID or online report ID" });
                }

                var studentReport = await _historyTeacherService.OnlineStudentReportEachQuiz(request.QuizId, request.OnlineReportId);
                _logger.LogInformation("GetOnlineStudentReport: Retrieved {Count} student records - QuizId={QuizId}",
                    studentReport?.Count ?? 0, request.QuizId);
                return Ok(studentReport ?? new List<ViewOnlineStudentReportEachQuizDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOnlineStudentReport: Error retrieving student report - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                    request?.QuizId, request?.OnlineReportId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get online question report for a specific quiz
        /// </summary>
        [HttpGet("online/question-report")]
        public async Task<IActionResult> GetOnlineQuestionReport([FromQuery] OnlineQuestionReportRequest request)
        {
            _logger.LogInformation("GetOnlineQuestionReport: Start - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                request?.QuizId, request?.OnlineReportId);
            try
            {
                if (request == null || request.QuizId <= 0 || request.OnlineReportId <= 0)
                {
                    _logger.LogWarning("GetOnlineQuestionReport: Invalid parameters - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                        request?.QuizId, request?.OnlineReportId);
                    return BadRequest(new { message = "Invalid quiz ID or online report ID" });
                }

                var questionReport = await _historyTeacherService.OnlineQuestionReportEachQuiz(request.QuizId, request.OnlineReportId);
                _logger.LogInformation("GetOnlineQuestionReport: Retrieved {Count} question statistics - QuizId={QuizId}",
                    questionReport?.Count ?? 0, request.QuizId);
                return Ok(questionReport ?? new List<ViewOnlineQuestionReportEachQuizDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOnlineQuestionReport: Error retrieving question report - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                    request?.QuizId, request?.OnlineReportId);
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
            _logger.LogInformation("CheckExpiredTime: Start - QuizId={QuizId}, QGId={QGId}", request?.QuizId, request?.QGId);
            try
            {
                if (request == null || request.QuizId <= 0 || request.QGId <= 0)
                {
                    _logger.LogWarning("CheckExpiredTime: Invalid parameters - QuizId={QuizId}, QGId={QGId}", request?.QuizId, request?.QGId);
                    return BadRequest(new { message = "Invalid quiz ID or QG ID" });
                }

                var isExpired = await _historyTeacherService.checkExpiredTime(request.QuizId, request.QGId);
                _logger.LogInformation("CheckExpiredTime: Quiz expired status={IsExpired} for QuizId={QuizId}, QGId={QGId}",
                    isExpired, request.QuizId, request.QGId);
                return Ok(new { isExpired, message = isExpired ? "Quiz has expired and status updated" : "Quiz is still active" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckExpiredTime: Error checking expired time for QuizId={QuizId}, QGId={QGId}",
                    request?.QuizId, request?.QGId);
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
                if (request == null || request.QuizId <= 0 || request.GroupId <= 0)
                {
                    _logger.LogWarning("EndNow: Invalid parameters - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
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
        /// Change the expired time for a quiz
        /// </summary>
        [HttpPut("change-expired-time")]
        public async Task<IActionResult> ChangeExpiredTime([FromBody] ChangeExpiredTimeRequest request)
        {
            _logger.LogInformation("ChangeExpiredTime: Start - QuizId={QuizId}, QGId={QGId}, NewExpiredTime={NewExpiredTime}",
                request?.QuizId, request?.QGId, request?.NewExpiredTime);
            try
            {
                if (request == null || request.QuizId <= 0 || request.QGId <= 0)
                {
                    _logger.LogWarning("ChangeExpiredTime: Invalid IDs - QuizId={QuizId}, QGId={QGId}", request?.QuizId, request?.QGId);
                    return BadRequest(new { message = "Invalid quiz ID or QG ID" });
                }

                var result = await _historyTeacherService.ChangeExpiredTime(request.QGId, request.QuizId, request.NewExpiredTime);

                switch (result)
                {
                    case ExpiredEnum.Success:
                        _logger.LogInformation("ChangeExpiredTime: Success - QuizId={QuizId}, QGId={QGId}", request.QuizId, request.QGId);
                        return Ok(new { message = "Expired time updated successfully" });

                    case ExpiredEnum.QuizGroupNotFound:
                        _logger.LogWarning("ChangeExpiredTime: Quiz group not found - QuizId={QuizId}, QGId={QGId}", request.QuizId, request.QGId);
                        return NotFound(new { message = "Quiz group not found" });

                    case ExpiredEnum.InvalidExpiredTime:
                        _logger.LogWarning("ChangeExpiredTime: Invalid expired time - NewExpiredTime={NewExpiredTime}", request.NewExpiredTime);
                        return BadRequest(new { message = "Expired time must be in the future" });

                    case ExpiredEnum.UpdateFailed:
                        _logger.LogWarning("ChangeExpiredTime: Update failed - QuizId={QuizId}, QGId={QGId}", request.QuizId, request.QGId);
                        return BadRequest(new { message = "Failed to update expired time" });

                    case ExpiredEnum.Error:
                    default:
                        _logger.LogError("ChangeExpiredTime: Unknown error - QuizId={QuizId}, QGId={QGId}", request.QuizId, request.QGId);
                        return StatusCode(500, new { message = "Internal server error" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeExpiredTime: Unexpected error - QuizId={QuizId}, QGId={QGId}", request?.QuizId, request?.QGId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Change the name of an offline report
        /// </summary>
        [HttpPut("offline/change-report-name")]
        public async Task<IActionResult> ChangeOfflineReportName([FromBody] ChangeOfflineReportNameRequest request)
        {
            _logger.LogInformation("ChangeOfflineReportName: Start - OfflineReportId={OfflineReportId}, NewName={NewName}",
                request?.OfflineReportId, request?.NewReportName);
            try
            {
                if (request == null || request.OfflineReportId <= 0)
                {
                    _logger.LogWarning("ChangeOfflineReportName: Invalid OfflineReportId={OfflineReportId}", request?.OfflineReportId);
                    return BadRequest(new { message = "Invalid offline report ID" });
                }

                if (string.IsNullOrWhiteSpace(request.NewReportName))
                {
                    _logger.LogWarning("ChangeOfflineReportName: Empty report name for OfflineReportId={OfflineReportId}", request.OfflineReportId);
                    return BadRequest(new { message = "Report name cannot be empty" });
                }

                var success = await _historyTeacherService.ChangeOfflineReport(request.OfflineReportId, request.NewReportName);
                if (success)
                {
                    _logger.LogInformation("ChangeOfflineReportName: Success - OfflineReportId={OfflineReportId}, NewName={NewName}",
                        request.OfflineReportId, request.NewReportName);
                    return Ok(new { message = "Offline report name updated successfully" });
                }
                else
                {
                    _logger.LogWarning("ChangeOfflineReportName: Failed to update - OfflineReportId={OfflineReportId}", request.OfflineReportId);
                    return NotFound(new { message = "Offline report not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeOfflineReportName: Error updating report name - OfflineReportId={OfflineReportId}",
                    request?.OfflineReportId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Change the name of an online report
        /// </summary>
        [HttpPut("online/change-report-name")]
        public async Task<IActionResult> ChangeOnlineReportName([FromBody] ChangeOnlineReportNameRequest request)
        {
            _logger.LogInformation("ChangeOnlineReportName: Start - OnlineReportId={OnlineReportId}, NewName={NewName}",
                request?.OnlineReportId, request?.NewReportName);
            try
            {
                if (request == null || request.OnlineReportId <= 0)
                {
                    _logger.LogWarning("ChangeOnlineReportName: Invalid OnlineReportId={OnlineReportId}", request?.OnlineReportId);
                    return BadRequest(new { message = "Invalid online report ID" });
                }

                if (string.IsNullOrWhiteSpace(request.NewReportName))
                {
                    _logger.LogWarning("ChangeOnlineReportName: Empty report name for OnlineReportId={OnlineReportId}", request.OnlineReportId);
                    return BadRequest(new { message = "Report name cannot be empty" });
                }

                var success = await _historyTeacherService.ChangeOnlineReportName(request.OnlineReportId, request.NewReportName);
                if (success)
                {
                    _logger.LogInformation("ChangeOnlineReportName: Success - OnlineReportId={OnlineReportId}, NewName={NewName}",
                        request.OnlineReportId, request.NewReportName);
                    return Ok(new { message = "Online report name updated successfully" });
                }
                else
                {
                    _logger.LogWarning("ChangeOnlineReportName: Failed to update - OnlineReportId={OnlineReportId}", request.OnlineReportId);
                    return NotFound(new { message = "Online report not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeOnlineReportName: Error updating report name - OnlineReportId={OnlineReportId}",
                    request?.OnlineReportId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
    // Request DTOs for the controller