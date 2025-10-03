using Capstone.DTOs.TeacherProfile;
using Capstone.Model;
using Capstone.Repositories.Profiles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherProfileController : ControllerBase
    {
        private readonly ILogger<TeacherProfileController> _logger;
        private readonly ITeacherProfileRepository _teacherProfileRepository;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TeacherProfileController(ILogger<TeacherProfileController> logger,
            ITeacherProfileRepository teacherProfileRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _teacherProfileRepository = teacherProfileRepository;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("getTeacherProfile/{teacherId}")]
        public async Task<IActionResult> getTeacherProfile(int teacherId)
        {
            _logger.LogInformation("getTeacherProfile: Start - TeacherId={TeacherId}", teacherId);
            try
            {
                var profile = await _teacherProfileRepository.getTeacherProfile(teacherId);
                if (profile == null)
                {
                    _logger.LogWarning("getTeacherProfile: Not found - TeacherId={TeacherId}", teacherId);
                    return NotFound(new { message = "Teacher profile not found" });
                }

               if(!string.IsNullOrEmpty(profile.AvatarURL))
                {
                    profile.AvatarURL = $"{Request.Scheme}://{Request.Host}/{profile.AvatarURL.Replace("\\", "/")}";
                }
                _logger.LogInformation("getTeacherProfile: Success - TeacherId={TeacherId}", teacherId);
                return Ok(new { message = "Get teacher profile successfully", profile });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getTeacherProfile: Error - TeacherId={TeacherId}", teacherId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("updateTeacherProfile")]
        public async Task<IActionResult> updateTeacherProfile([FromForm] TeacherProfileUpdateDTO dto)
        {
            _logger.LogInformation("updateTeacherProfile: Start - TeacherId={TeacherId}", dto?.TeacherId);
            try
            {
                if (dto == null)
                {
                    _logger.LogWarning("updateTeacherProfile: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                var folderName = _configuration["UploadSettings:AvatarFolder"];
                var uploadsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, folderName);
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var extension = Path.GetExtension(dto.FormFile.FileName);
                var fileName = $"{dto.TeacherId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await dto.FormFile.CopyToAsync(stream);
                }

                var model = new TeacherProfileModel
                {
                    TeacherId = dto.TeacherId,
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber,
                    OrganizationName = dto.OrganizationName,
                    OrganizationAddress = dto.OrganizationAddress,
                    AvatarURL = Path.Combine(folderName, fileName)
                };

                var updated = await _teacherProfileRepository.updateTeacherProfile(model);
                if (updated == null)
                {
                    _logger.LogWarning("updateTeacherProfile: Update failed for TeacherId={TeacherId}", dto.TeacherId);
                    return StatusCode(500, new { message = "Failed to update profile" });
                }

                if (!string.IsNullOrEmpty(updated.oldAvatar))
                {
                    var oldAvatarPath = Path.Combine(_webHostEnvironment.ContentRootPath, updated.oldAvatar);
                    if (System.IO.File.Exists(oldAvatarPath)) System.IO.File.Delete(oldAvatarPath);
                }

                _logger.LogInformation("updateTeacherProfile: Success - TeacherId={TeacherId}", dto.TeacherId);
                return Ok(new { message = "Update teacher profile successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateTeacherProfile: Error - TeacherId={TeacherId}", dto?.TeacherId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
