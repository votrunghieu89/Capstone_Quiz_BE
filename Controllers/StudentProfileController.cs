using Capstone.DTOs.StudentProfile;
using Capstone.Model;
using Capstone.Repositories.Profiles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

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
        public StudentProfileController(ILogger<StudentProfileController> logger,
            IStudentProfileRepository studentProfileRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _studentProfileRepository = studentProfileRepository;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("getStudentProfile/{studentId}")]
        public async Task<IActionResult> getStudentProfile(int studentId)
        {
            _logger.LogInformation("getStudentProfile: Start - StudentId={StudentId}", studentId);
            try
            {
                var studentProfile = await _studentProfileRepository.getStudentProfile(studentId);
                if (studentProfile == null)
                {
                    _logger.LogWarning("getStudentProfile: Profile not found - StudentId={StudentId}", studentId);
                    return NotFound(new { message = "Student profile not found" });
                }
                if (!string.IsNullOrEmpty(studentProfile.AvatarURL))
                {
                    studentProfile.AvatarURL = $"{Request.Scheme}://{Request.Host}/{studentProfile.AvatarURL.Replace("\\", "/")}";
                }
                StudentProfileModel student = new StudentProfileModel
                {
                    StudentId = studentProfile.StudentId,
                    FullName = studentProfile.FullName,
                    IdUnique = studentProfile.IdUnique,
                    AvatarURL = studentProfile.AvatarURL ?? string.Empty,
                };

                _logger.LogInformation("getStudentProfile: Success - StudentId={StudentId}, AvatarURL={AvatarURL}", studentId, student.AvatarURL);
                return Ok(new { message = "Get student profile successfully", profile = student });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getStudentProfile: Error while retrieving profile for StudentId={StudentId}", studentId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("updateStudentProfile")]
        public async Task<IActionResult> updateStudentProfile([FromForm] StudenProfileUpdateDTO studentProfile)
        {
            _logger.LogInformation("updateStudentProfile: Start - StudentId={StudentId}", studentProfile?.StudentId);
            try
            {
                if (studentProfile == null)
                {
                    _logger.LogWarning("updateStudentProfile: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                var folderName = _configuration["UploadSettings:AvatarFolder"];
                var uploadsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, folderName);
                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogDebug("updateStudentProfile: Creating uploads folder at {UploadsFolder}", uploadsFolder);
                    Directory.CreateDirectory(uploadsFolder);
                }

                var extension = Path.GetExtension(studentProfile.FormFile.FileName);
                var FileName = $"{studentProfile.StudentId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, FileName);

                _logger.LogDebug("updateStudentProfile: Saving uploaded avatar to {FilePath}", filePath);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await studentProfile.FormFile.CopyToAsync(stream);
                }

                var studentProfileModel = new Capstone.Model.StudentProfileModel
                {
                    StudentId = studentProfile.StudentId,
                    FullName = studentProfile.FullName,
                    AvatarURL = Path.Combine(folderName, FileName)
                };

                _logger.LogDebug("updateStudentProfile: Updating DB for StudentId={StudentId}", studentProfileModel.StudentId);
                var updatedProfile = await _studentProfileRepository.updateStudentProfile(studentProfileModel);

                if (updatedProfile == null)
                {
                    _logger.LogWarning("updateStudentProfile: Repository update returned null for StudentId={StudentId}", studentProfileModel.StudentId);
                    return StatusCode(500, new { message = "Failed to update profile" });
                }

                if (!string.IsNullOrEmpty(updatedProfile.oldAvatar))
                {
                    var oldAvatarPath = Path.Combine(_webHostEnvironment.ContentRootPath, updatedProfile.oldAvatar);
                    _logger.LogDebug("updateStudentProfile: Deleting old avatar at {OldAvatarPath} if exists", oldAvatarPath);
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                        _logger.LogInformation("updateStudentProfile: Deleted old avatar for StudentId={StudentId}", studentProfileModel.StudentId);
                    }
                    else
                    {
                        _logger.LogDebug("updateStudentProfile: Old avatar file not found at {OldAvatarPath}", oldAvatarPath);
                    }
                }

                _logger.LogInformation("updateStudentProfile: Success - StudentId={StudentId}", studentProfileModel.StudentId);
                return Ok(new { message = "Update student profile successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateStudentProfile: Error updating profile for StudentId={StudentId}", studentProfile?.StudentId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
