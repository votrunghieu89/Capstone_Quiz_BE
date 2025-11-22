using Capstone.DTOs.Reports.Student;
using Capstone.Repositories;
using Capstone.Repositories.Histories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentReportController : ControllerBase
    {
        private readonly ILogger<StudentReportController> _logger;
        private readonly IStudentReportRepository _studentReportService;
        private readonly IAWS _S3;
        public StudentReportController(ILogger<StudentReportController> logger, IStudentReportRepository studentReportService, IAWS S3)
        {
            _logger = logger;
            _studentReportService = studentReportService;
            _S3 = S3;
        }

        // ===== GET METHODS =====

        /// <summary>
        /// Get all completed public quizzes for a student
        /// </summary>
        [HttpGet("public-quizzes/{studentId:int}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetAllCompletedPublicQuizzes(int studentId)
        {
            _logger.LogInformation("GetAllCompletedPublicQuizzes: Start - StudentId={StudentId}", studentId);
            try
            {
                if (studentId <= 0)
                {
                    _logger.LogWarning("GetAllCompletedPublicQuizzes: Invalid StudentId={StudentId}", studentId);
                    return BadRequest(new { message = "ID học viên không hợp lệ" });
                }

                var publicQuizzes = await _studentReportService.GetAllCompletedPublicQuizzes(studentId);
                foreach (var quiz in publicQuizzes)
                {
                    quiz.AvatarURL = await _S3.ReadImage(quiz.AvatarURL);
                }
                _logger.LogInformation("GetAllCompletedPublicQuizzes: Retrieved {Count} public quizzes for StudentId={StudentId}",
                    publicQuizzes?.Count ?? 0, studentId);
                return Ok(publicQuizzes ?? new List<GetAllCompletedPublicQuizzesDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllCompletedPublicQuizzes: Error retrieving public quizzes for StudentId={StudentId}", studentId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get all completed private quizzes for a student
        /// </summary>
        [HttpGet("private-quizzes/{studentId:int}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetAllCompletedPrivateQuizzes(int studentId)
        {
            _logger.LogInformation("GetAllCompletedPrivateQuizzes: Start - StudentId={StudentId}", studentId);
            try
            {
                if (studentId <= 0)
                {
                    _logger.LogWarning("GetAllCompletedPrivateQuizzes: Invalid StudentId={StudentId}", studentId);
                    return BadRequest(new { message = "ID học viên không hợp lệ" });
                }

                var privateQuizzes = await _studentReportService.GetAllCompletedPrivateQuizzes(studentId);
                foreach (var quiz in privateQuizzes)
                {
                    quiz.AvatarURL = await _S3.ReadImage(quiz.AvatarURL);
                }
                _logger.LogInformation("GetAllCompletedPrivateQuizzes: Retrieved {Count} private quizzes for StudentId={StudentId}",
                    privateQuizzes?.Count ?? 0, studentId);
                return Ok(privateQuizzes ?? new List<GetAllCompletedPrivateQuizzesDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllCompletedPrivateQuizzes: Error retrieving private quizzes for StudentId={StudentId}", studentId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get detailed information about a completed quiz
        /// </summary>
        [HttpGet("quiz-detail")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetDetailOfCompletedQuiz([FromQuery] DetailOfCompletedQuizRequest request)
        {
            _logger.LogInformation("GetDetailOfCompletedQuiz: Start - StudentId={StudentId}, QuizId={QuizId}, CreateAt={CreateAt}",
                request?.StudentId, request?.QuizId, request?.CreateAt);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("GetDetailOfCompletedQuiz: Request is null");
                    return BadRequest(new { message = "Request body is required" });
                }

                if (request.StudentId <= 0 || request.QuizId <= 0)
                {
                    _logger.LogWarning("GetDetailOfCompletedQuiz: Invalid parameters - StudentId={StudentId}, QuizId={QuizId}",
                        request.StudentId, request.QuizId);
                    return BadRequest(new { message = "Invalid student ID or quiz ID" });
                }

                if (request.CreateAt == default(DateTime))
                {
                    _logger.LogWarning("GetDetailOfCompletedQuiz: Invalid CreateAt date - StudentId={StudentId}, QuizId={QuizId}",
                        request.StudentId, request.QuizId);
                    return BadRequest(new { message = "Invalid creation date" });
                }

                var quizDetail = await _studentReportService.DetailOfCompletedQuiz(request.StudentId, request.QuizId, request.CreateAt);
                if (quizDetail == null)
                {
                    _logger.LogWarning("GetDetailOfCompletedQuiz: Quiz detail not found - StudentId={StudentId}, QuizId={QuizId}, CreateAt={CreateAt}",
                        request.StudentId, request.QuizId, request.CreateAt);
                    return NotFound(new { message = "Không tìm thấy chi tiết bài kiểm tra" });
                }

                _logger.LogInformation("GetDetailOfCompletedQuiz: Success - StudentId={StudentId}, QuizId={QuizId}, TotalQuestions={TotalQuestions}",
                    request.StudentId, request.QuizId, quizDetail.TotalQuestions);
                return Ok(quizDetail);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Không tìm thấy kết quả quiz này"))
                {
                    _logger.LogWarning("GetDetailOfCompletedQuiz: Quiz result not found - StudentId={StudentId}, QuizId={QuizId}, CreateAt={CreateAt}",
                        request?.StudentId, request?.QuizId, request?.CreateAt);
                    return NotFound(new { message = "Không tìm thấy kết quả bài kiểm tra với các tham số được chỉ định" });
                }

                _logger.LogError(ex, "GetDetailOfCompletedQuiz: Error retrieving quiz detail - StudentId={StudentId}, QuizId={QuizId}, CreateAt={CreateAt}",
                    request?.StudentId, request?.QuizId, request?.CreateAt);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get detailed information about a completed quiz using path parameters
        /// </summary>
        [HttpGet("quiz-detail/{studentId:int}/{quizId:int}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetDetailOfCompletedQuizByPath(int studentId, int quizId, [FromQuery] DateTime createAt)
        {
            _logger.LogInformation("GetDetailOfCompletedQuizByPath: Start - StudentId={StudentId}, QuizId={QuizId}, CreateAt={CreateAt}",
                studentId, quizId, createAt);
            try
            {
                if (studentId <= 0 || quizId <= 0)
                {
                    _logger.LogWarning("GetDetailOfCompletedQuizByPath: Invalid parameters - StudentId={StudentId}, QuizId={QuizId}",
                        studentId, quizId);
                    return BadRequest(new { message = "Invalid student ID or quiz ID" });
                }

                if (createAt == default(DateTime))
                {
                    _logger.LogWarning("GetDetailOfCompletedQuizByPath: Invalid CreateAt date - StudentId={StudentId}, QuizId={QuizId}",
                        studentId, quizId);
                    return BadRequest(new { message = "Tham số truy vấn CreateAt là bắt buộc và phải là ngày hợp lệ" });
                }

                var quizDetail = await _studentReportService.DetailOfCompletedQuiz(studentId, quizId, createAt);
                if (quizDetail == null)
                {
                    _logger.LogWarning("GetDetailOfCompletedQuizByPath: Quiz detail not found - StudentId={StudentId}, QuizId={QuizId}, CreateAt={CreateAt}",
                        studentId, quizId, createAt);
                    return NotFound(new { message = "Không tìm thấy chi tiết bài kiểm tra" });
                }

                _logger.LogInformation("GetDetailOfCompletedQuizByPath: Success - StudentId={StudentId}, QuizId={QuizId}, TotalQuestions={TotalQuestions}",
                    studentId, quizId, quizDetail.TotalQuestions);
                return Ok(quizDetail);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Không tìm thấy kết quả quiz này"))
                {
                    _logger.LogWarning("GetDetailOfCompletedQuizByPath: Quiz result not found - StudentId={StudentId}, QuizId={QuizId}, CreateAt={CreateAt}",
                        studentId, quizId, createAt);
                    return NotFound(new { message = "Không tìm thấy kết quả bài kiểm tra với các tham số được chỉ định" });
                }

                _logger.LogError(ex, "GetDetailOfCompletedQuizByPath: Error retrieving quiz detail - StudentId={StudentId}, QuizId={QuizId}, CreateAt={CreateAt}",
                    studentId, quizId, createAt);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }
    }
}
