using Capstone.DTOs.Reports.Teacher;
using Capstone.DTOs.Reports.Teacher.OfflineReport;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
using Capstone.ENUMs;
using Capstone.Repositories;
using Capstone.Repositories.Histories;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IAWS _S3;

        public TeacherReportController(ILogger<TeacherReportController> logger, ITeacherReportRepository historyTeacherService, IAWS S3)
        {
            _logger = logger;
            _historyTeacherService = historyTeacherService;
            _S3 = S3;
        }

        // ===== GET METHODS =====

        /// <summary>
        /// Get all offline quiz reports for a teacher
        /// </summary>
        [HttpGet("offline/quiz-reports/{teacherId:int}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetOfflineQuizReports(int teacherId)
        {
            _logger.LogInformation("GetOfflineQuizReports: Start - TeacherId={TeacherId}", teacherId);
            try
            {
                if (teacherId <= 0)
                {
                    _logger.LogWarning("GetOfflineQuizReports: Invalid TeacherId={TeacherId}", teacherId);
                    return BadRequest(new { message = "ID giáo viên không hợp lệ" });
                }

                var offlineReports = await _historyTeacherService.GetOfflineQuizz(teacherId);
                _logger.LogInformation("GetOfflineQuizReports: Retrieved {Count} offline reports for TeacherId={TeacherId}", offlineReports?.Count ?? 0, teacherId);
                return Ok(offlineReports ?? new List<ViewAllOfflineReportDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOfflineQuizReports: Error retrieving offline reports for TeacherId={TeacherId}", teacherId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get detailed offline report for a specific quiz
        /// </summary>
        [HttpGet("offline/detail-report")]
        [Authorize(Roles = "Teacher")]
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
                    return BadRequest(new { message = "ID báo cáo offline hoặc ID bài kiểm tra không hợp lệ" });
                }

                var detailReport = await _historyTeacherService.OfflineDetailReportEachQuiz(request.OfflineReportId, request.QuizId);
                if (detailReport == null)
                {
                    _logger.LogWarning("GetOfflineDetailReport: Report not found - OfflineReportId={OfflineReportId}, QuizId={QuizId}",
                        request.OfflineReportId, request.QuizId);
                    return NotFound(new { message = "Không tìm thấy báo cáo offline" });
                }

                _logger.LogInformation("GetOfflineDetailReport: Success - OfflineReportId={OfflineReportId}, QuizId={QuizId}",
                    request.OfflineReportId, request.QuizId);
                return Ok(detailReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOfflineDetailReport: Error retrieving detail report - OfflineReportId={OfflineReportId}, QuizId={QuizId}",
                    request?.OfflineReportId, request?.QuizId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get offline student report for a specific quiz
        /// </summary>
        [HttpGet("offline/student-report")]
        [Authorize(Roles = "Teacher")]
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
                    return BadRequest(new { message = "ID bài kiểm tra, QG ID hoặc ID nhóm không hợp lệ" });
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
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get offline question report for a specific quiz
        /// </summary>
        [HttpGet("offline/question-report")]
        [Authorize(Roles = "Teacher")]
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
                    return BadRequest(new { message = "ID bài kiểm tra, QG ID hoặc ID nhóm không hợp lệ" });
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
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get all online quiz reports for a teacher
        /// </summary>
        [HttpGet("online/quiz-reports/{teacherId:int}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetOnlineQuizReports(int teacherId)
        {
            _logger.LogInformation("GetOnlineQuizReports: Start - TeacherId={TeacherId}", teacherId);
            try
            {
                if (teacherId <= 0)
                {
                    _logger.LogWarning("GetOnlineQuizReports: Invalid TeacherId={TeacherId}", teacherId);
                    return BadRequest(new { message = "ID giáo viên không hợp lệ" });
                }

                var onlineReports = await _historyTeacherService.GetOnlineQuiz(teacherId);
                _logger.LogInformation("GetOnlineQuizReports: Retrieved {Count} online reports for TeacherId={TeacherId}", onlineReports?.Count ?? 0, teacherId);
                return Ok(onlineReports ?? new List<ViewAllOnlineReportDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOnlineQuizReports: Error retrieving online reports for TeacherId={TeacherId}", teacherId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get detailed online report for a specific quiz
        /// </summary>
        [HttpGet("online/detail-report")]
        [Authorize(Roles = "Teacher")]
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
                    return BadRequest(new { message = "ID bài kiểm tra hoặc ID báo cáo online không hợp lệ" });
                }

                var detailReport = await _historyTeacherService.OnlineDetailReportEachQuiz(request.QuizId, request.OnlineReportId);
                if (detailReport == null)
                {
                    _logger.LogWarning("GetOnlineDetailReport: Report not found - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                        request.QuizId, request.OnlineReportId);
                    return NotFound(new { message = "Không tìm thấy báo cáo online" });
                }

                _logger.LogInformation("GetOnlineDetailReport: Success - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                    request.QuizId, request.OnlineReportId);
                return Ok(detailReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOnlineDetailReport: Error retrieving detail report - QuizId={QuizId}, OnlineReportId={OnlineReportId}",
                    request?.QuizId, request?.OnlineReportId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get online student report for a specific quiz
        /// </summary>
        [HttpGet("online/student-report")]
        [Authorize(Roles = "Teacher")]
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
                    return BadRequest(new { message = "ID bài kiểm tra hoặc ID báo cáo online không hợp lệ" });
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
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get online question report for a specific quiz
        /// </summary>
        [HttpGet("online/question-report")]
        [Authorize(Roles = "Teacher")]
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
                    return BadRequest(new { message = "ID bài kiểm tra hoặc ID báo cáo online không hợp lệ" });
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
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get detailed information about a specific question
        /// </summary>
        [HttpGet("question-detail/{questionId:int}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ViewDetailOfQuestion(int questionId)
        {
            _logger.LogInformation("ViewDetailOfQuestion: Start - QuestionId={QuestionId}", questionId);
            try
            {
                if (questionId <= 0)
                {
                    _logger.LogWarning("ViewDetailOfQuestion: Invalid QuestionId={QuestionId}", questionId);
                    return BadRequest(new { message = "ID câu hỏi không hợp lệ" });
                }

                var questionDetail = await _historyTeacherService.ViewDetailOfQuestion(questionId);
                if (questionDetail == null)
                {
                    _logger.LogWarning("ViewDetailOfQuestion: Question not found - QuestionId={QuestionId}", questionId);
                    return NotFound(new { message = "Không tìm thấy câu hỏi" });
                }

                _logger.LogInformation("ViewDetailOfQuestion: Success - QuestionId={QuestionId}", questionId);
                return Ok(questionDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewDetailOfQuestion: Error retrieving question detail for QuestionId={QuestionId}", questionId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        // ===== POST METHODS =====

        /// <summary>
        /// Check if a quiz has expired and update status if needed
        /// </summary>
        [HttpPost("check-expired")]
        [Authorize(Roles = "Teacher")]
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
                var isExpired = await _historyTeacherService.checkExpiredTime(request.QGId, request.QuizId);
                _logger.LogInformation("CheckExpiredTime: Quiz expired status={IsExpired} for QuizId={QuizId}, QGId={QGId}",
                    isExpired, request.QuizId, request.QGId);
                return Ok(new { isExpired, message = isExpired ? "Bài kiểm tra đã hết hạn và trạng thái đã được cập nhật" : "Bài kiểm tra vẫn còn hoạt động" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckExpiredTime: Error checking expired time for QuizId={QuizId}, QGId={QGId}",
                    request?.QuizId, request?.QGId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// End a quiz immediately by setting status to Completed
        /// </summary>
        [HttpPost("end-now")]
        [Authorize(Roles = "Teacher")]
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
                    return Ok(new { message = "Kết thúc bài kiểm tra thành công" });
                }
                else
                {
                    _logger.LogWarning("EndNow: Failed to end quiz - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                    return BadRequest(new { message = "Kết thúc bài kiểm tra thất bại" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EndNow: Error ending quiz - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        // ===== PUT METHODS =====

        /// <summary>
        /// Change the expired time for a quiz
        /// </summary>
        [HttpPut("change-expired-time")]
        [Authorize(Roles = "Teacher")]
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
                        return Ok(new { message = "Cập nhật thời gian hết hạn thành công" });

                    case ExpiredEnum.QuizGroupNotFound:
                        _logger.LogWarning("ChangeExpiredTime: Quiz group not found - QuizId={QuizId}, QGId={QGId}", request.QuizId, request.QGId);
                        return NotFound(new { message = "Không tìm thấy nhóm bài kiểm tra" });

                    case ExpiredEnum.InvalidExpiredTime:
                        _logger.LogWarning("ChangeExpiredTime: Invalid expired time - NewExpiredTime={NewExpiredTime}", request.NewExpiredTime);
                        return BadRequest(new { message = "Thời gian hết hạn phải là thời gian tương lai" });

                    case ExpiredEnum.UpdateFailed:
                        _logger.LogWarning("ChangeExpiredTime: Update failed - QuizId={QuizId}, QGId={QGId}", request.QuizId, request.QGId);
                        return BadRequest(new { message = "Cập nhật thời gian hết hạn thất bại" });

                    case ExpiredEnum.Error:
                    default:
                        _logger.LogError("ChangeExpiredTime: Unknown error - QuizId={QuizId}, QGId={QGId}", request.QuizId, request.QGId);
                        return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeExpiredTime: Unexpected error - QuizId={QuizId}, QGId={QGId}", request?.QuizId, request?.QGId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Change the name of an offline report
        /// </summary>
        [HttpPut("offline/change-report-name")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ChangeOfflineReportName([FromBody] ChangeOfflineReportNameRequest request)
        {
            _logger.LogInformation("ChangeOfflineReportName: Start - OfflineReportId={OfflineReportId}, NewName={NewName}",
                request?.OfflineReportId, request?.NewReportName);
            try
            {
                if (request == null || request.OfflineReportId <= 0)
                {
                    _logger.LogWarning("ChangeOfflineReportName: Invalid OfflineReportId={OfflineReportId}", request?.OfflineReportId);
                    return BadRequest(new { message = "ID báo cáo offline không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(request.NewReportName))
                {
                    _logger.LogWarning("ChangeOfflineReportName: Empty report name for OfflineReportId={OfflineReportId}", request.OfflineReportId);
                    return BadRequest(new { message = "Tên báo cáo không được để trống" });
                }

                var success = await _historyTeacherService.ChangeOfflineReport(request.OfflineReportId, request.NewReportName);
                if (success)
                {
                    _logger.LogInformation("ChangeOfflineReportName: Success - OfflineReportId={OfflineReportId}, NewName={NewName}",
                        request.OfflineReportId, request.NewReportName);
                    return Ok(new { message = "Cập nhật tên báo cáo offline thành công" });
                }
                else
                {
                    _logger.LogWarning("ChangeOfflineReportName: Failed to update - OfflineReportId={OfflineReportId}", request.OfflineReportId);
                    return NotFound(new { message = "Không tìm thấy báo cáo offline hoặc cập nhật thất bại" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeOfflineReportName: Error updating report name - OfflineReportId={OfflineReportId}",
                    request?.OfflineReportId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Change the name of an online report
        /// </summary>
        [HttpPut("online/change-report-name")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ChangeOnlineReportName([FromBody] ChangeOnlineReportNameRequest request)
        {
            _logger.LogInformation("ChangeOnlineReportName: Start - OnlineReportId={OnlineReportId}, NewName={NewName}",
                request?.OnlineReportId, request?.NewReportName);
            try
            {
                if (request == null || request.OnlineReportId <= 0)
                {
                    _logger.LogWarning("ChangeOnlineReportName: Invalid OnlineReportId={OnlineReportId}", request?.OnlineReportId);
                    return BadRequest(new { message = "ID báo cáo online không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(request.NewReportName))
                {
                    _logger.LogWarning("ChangeOnlineReportName: Empty report name for OnlineReportId={OnlineReportId}", request.OnlineReportId);
                    return BadRequest(new { message = "Tên báo cáo không được để trống" });
                }

                var success = await _historyTeacherService.ChangeOnlineReportName(request.OnlineReportId, request.NewReportName);
                if (success)
                {
                    _logger.LogInformation("ChangeOnlineReportName: Success - OnlineReportId={OnlineReportId}, NewName={NewName}",
                        request.OnlineReportId, request.NewReportName);
                    return Ok(new { message = "Cập nhật tên báo cáo online thành công" });
                }
                else
                {
                    _logger.LogWarning("ChangeOnlineReportName: Failed to update - OnlineReportId={OnlineReportId}", request.OnlineReportId);
                    return NotFound(new { message = "Không tìm thấy báo cáo online hoặc cập nhật thất bại" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeOnlineReportName: Error updating report name - OnlineReportId={OnlineReportId}",
                    request?.OnlineReportId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }
    }
}
    // Request DTOs for the controller