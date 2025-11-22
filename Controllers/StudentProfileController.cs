using Capstone.DTOs.StudentProfile;
using Capstone.Model;
using Capstone.Repositories;
using Capstone.Repositories.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentProfileController : ControllerBase
    {
        private readonly ILogger<StudentProfileController> _logger;
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAWS _S3;
        public StudentProfileController(ILogger<StudentProfileController> logger,
            IStudentProfileRepository studentProfileRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IAWS S3)
        {
            _logger = logger;
            _studentProfileRepository = studentProfileRepository;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _S3 = S3;
        }

        // ===== GET METHODS =====
        [HttpGet("getStudentProfile/{studentId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> getStudentProfile(int studentId)
        {
            _logger.LogInformation("getStudentProfile: Start - StudentId={StudentId}", studentId);
            try
            {
                var studentProfile = await _studentProfileRepository.getStudentProfile(studentId);
                if (studentProfile == null)
                {
                    _logger.LogWarning("getStudentProfile: Profile not found - StudentId={StudentId}", studentId);
                    return NotFound(new { message = "Không tìm thấy hồ sơ học viên" });
                }
                if (!string.IsNullOrEmpty(studentProfile.AvatarURL))
                {
                    studentProfile.AvatarURL = await _S3.ReadImage(studentProfile.AvatarURL);
                }
                StudentProfileModel student = new StudentProfileModel
                {
                    StudentId = studentProfile.StudentId,
                    FullName = studentProfile.FullName,
                    IdUnique = studentProfile.IdUnique,
                    AvatarURL = studentProfile.AvatarURL ?? string.Empty,
                };

                _logger.LogInformation("getStudentProfile: Success - StudentId={StudentId}, AvatarURL={AvatarURL}", studentId, student.AvatarURL);
                return Ok(new { message = "Lấy hồ sơ học viên thành công", profile = student });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getStudentProfile: Error while retrieving profile for StudentId={StudentId}", studentId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        // ===== POST METHODS =====
        [HttpPost("updateStudentProfile")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> updateStudentProfile([FromForm] StudenProfileUpdateDTO studentProfile)
        {
            _logger.LogInformation("updateStudentProfile: Start - StudentId={StudentId}", studentProfile?.StudentId);
            try
            {
                if (studentProfile == null)
                {
                    _logger.LogWarning("updateStudentProfile: Request body null");
                    return BadRequest(new { message = "Yêu cầu phải có dữ liệu đầu vào." });
                }
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var studentProfileModel = new StudentProfileModel();

                if (studentProfile.FormFile != null)
                {
                    var profileImage = await _S3.UploadProfileImageToS3(studentProfile.FormFile);

                    studentProfileModel = new Capstone.Model.StudentProfileModel
                    {
                        StudentId = studentProfile.StudentId,
                        FullName = studentProfile.FullName,
                        AvatarURL = profileImage
                    };
                }
                else
                {
                    studentProfileModel = new Capstone.Model.StudentProfileModel
                    {
                        StudentId = studentProfile.StudentId,
                        FullName = studentProfile.FullName,
                        AvatarURL = null
                    };
                }

                _logger.LogDebug("updateStudentProfile: Updating DB for StudentId={StudentId}", studentProfileModel.StudentId);
                var updatedProfile = await _studentProfileRepository.updateStudentProfile(studentProfileModel,accountId,ipAddess);

                if (updatedProfile == null)
                {
                    _logger.LogWarning("updateStudentProfile: Repository update returned null for StudentId={StudentId}", studentProfileModel.StudentId);
                    return StatusCode(500, new { message = "Cập nhật hồ sơ thất bại" });
                }

                if (studentProfile.FormFile != null && !string.IsNullOrEmpty(updatedProfile.oldAvatar))
                {
                    bool isdeleteImage = await _S3.DeleteImage(updatedProfile.oldAvatar);
                }

                _logger.LogInformation("updateStudentProfile: Success - StudentId={StudentId}", studentProfileModel.StudentId);
                return Ok(new { message = "Cập nhật hồ sơ học viên thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateStudentProfile: Error updating profile for StudentId={StudentId}", studentProfile?.StudentId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }
    }
}
