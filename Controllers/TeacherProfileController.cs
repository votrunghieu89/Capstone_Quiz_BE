using Capstone.DTOs.TeacherProfile;
using Capstone.Model;
using Capstone.Repositories;
using Capstone.Repositories.Profiles;
using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IAWS _S3;

        public TeacherProfileController(ILogger<TeacherProfileController> logger,
            ITeacherProfileRepository teacherProfileRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IAWS S3)
        {
            _logger = logger;
            _teacherProfileRepository = teacherProfileRepository;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _S3 = S3;
        }

        // ===== GET METHODS =====
        [HttpGet("getTeacherProfile/{teacherId}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> getTeacherProfile(int teacherId)
        {
            _logger.LogInformation("getTeacherProfile: Start - TeacherId={TeacherId}", teacherId);
            try
            {
                var profile = await _teacherProfileRepository.getTeacherProfile(teacherId);
                if (profile == null)
                {
                    _logger.LogWarning("getTeacherProfile: Not found - TeacherId={TeacherId}", teacherId);
                    return NotFound(new { message = "Không tìm thấy hồ sơ giáo viên" });
                }

               if(!string.IsNullOrEmpty(profile.AvatarURL))
                {
                    profile.AvatarURL = await _S3.ReadImage(profile.AvatarURL);
                }
                _logger.LogInformation("getTeacherProfile: Success - TeacherId={TeacherId}", teacherId);
                return Ok(new { message = "Lấy hồ sơ giáo viên thành công", profile });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getTeacherProfile: Error - TeacherId={TeacherId}", teacherId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        // ===== POST METHODS =====
        [HttpPost("updateTeacherProfile")]
        public async Task<IActionResult> updateTeacherProfile([FromForm] TeacherProfileUpdateDTO dto)
        {
            _logger.LogInformation("updateTeacherProfile: Start - TeacherId={TeacherId}", dto?.TeacherId);
            try
            {
                if (dto == null)
                {
                    _logger.LogWarning("updateTeacherProfile: Request body null");
                    return BadRequest(new { message = "Yêu cầu phải có dữ liệu đầu vào." });
                }
                var model = new TeacherProfileModel();
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();

                if (dto.FormFile != null)
                {
                   
                    var profileImage = await _S3.UploadProfileImageToS3(dto.FormFile);
                    model = new TeacherProfileModel
                    {
                        TeacherId = dto.TeacherId,
                        FullName = dto.FullName,
                        PhoneNumber = dto.PhoneNumber,
                        OrganizationName = dto.OrganizationName,
                        OrganizationAddress = dto.OrganizationAddress,
                        AvatarURL = profileImage
                    };
                }
                else
                {
                    model = new TeacherProfileModel
                    {
                        TeacherId = dto.TeacherId,
                        FullName = dto.FullName,
                        PhoneNumber = dto.PhoneNumber,
                        OrganizationName = dto.OrganizationName,
                        OrganizationAddress = dto.OrganizationAddress,
                        AvatarURL = null,
                    };
                }

                var updated = await _teacherProfileRepository.updateTeacherProfile(model, accountId, ipAddess);
                if (updated == null)
                {
                    _logger.LogWarning("updateTeacherProfile: Update failed for TeacherId={TeacherId}", dto.TeacherId);
                    return StatusCode(500, new { message = "Cập nhật hồ sơ thất bại" });
                }

                if (dto.FormFile != null && !string.IsNullOrEmpty(updated.oldAvatar))
                {
                    bool isdeleteImage = await _S3.DeleteImage(updated.oldAvatar);
                }

                _logger.LogInformation("updateTeacherProfile: Success - TeacherId={TeacherId}", dto.TeacherId);
                return Ok(new { message = "Cập nhật hồ sơ giáo viên thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateTeacherProfile: Error - TeacherId={TeacherId}", dto?.TeacherId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }
    }
}
