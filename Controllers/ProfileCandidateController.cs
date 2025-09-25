using Capstone.DTOs.CandidateProfile;
using Capstone.Model;
using Capstone.Model.Profile;
using Capstone.Repositories.Profile;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileCandidateController : ControllerBase
    {
        private readonly ILogger<ProfileCandidateController> _logger;
        private readonly ICandidatePofileRepository _candidatePofileRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        public ProfileCandidateController(ILogger<ProfileCandidateController> logger,
            ICandidatePofileRepository candidatePofileRepository, IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _logger = logger;
            _candidatePofileRepository = candidatePofileRepository;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        [HttpDelete("deleteCV/{CVId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> deleteCV(int CVId)
        {
            try
            {
                var result = await _candidatePofileRepository.deleteCV(CVId);
                if (result != null)
                {
                    var fullPath = Path.Combine(_webHostEnvironment.ContentRootPath, result.FilePath);
                    Console.WriteLine(fullPath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        _logger.LogInformation("Deleting file at path: {FilePath}", fullPath);
                        System.IO.File.Delete(fullPath);
                    }
                    return Ok(new { message = "Delete CV successful" });
                }
                else
                {
                    return BadRequest(new { message = "No CV found to delete" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in deleteCV for CVId: {CVId}", CVId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("uploadCV")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> uploadCV([FromForm] ProfileCandidateUploadCVDTO profileCandidateUploadCVDTO)
        {
            try
            {
                bool isConnect = await _candidatePofileRepository.checkConnection();
                if (!isConnect)
                {
                    return StatusCode(500, new { message = "Database connection failed" });
                }
                var folderName = _configuration["UploadSettings:CVFolder"];
                var uploadsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, folderName);
                //var pathCombine = Path.Combine("E:\\Capstone\\Capstone\\CVPDF");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var extension = Path.GetExtension(profileCandidateUploadCVDTO.FormFile.FileName); // ".pdf"
                var FileName = $"{profileCandidateUploadCVDTO.AccountId}_{Guid.NewGuid()}";
                var FileExtension = $"{FileName}{extension}";
                var filePath = Path.Combine(uploadsFolder, FileExtension);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await profileCandidateUploadCVDTO.FormFile.CopyToAsync(stream);
                }
                var pcAId = await _candidatePofileRepository.getPACIDbyAccountId(profileCandidateUploadCVDTO.AccountId);
                var cVModel = new CVsModel()
                {
                    PCAId = pcAId,
                    FileName = FileName,
                    FilePath = Path.Combine(folderName, FileExtension),
                    CreatedAt = DateTime.Now,
                };
                bool result = await _candidatePofileRepository.uploadCV(cVModel);
                if (result)
                {
                    return Ok(new { message = "Upload CV successful", CsV = cVModel });
                }
                return BadRequest(new { message = "Upload CV failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in uploadCV");
                return StatusCode(500, new { message = "Internal server error" });
            }
        } 

        [HttpPost("updateProfileCandidate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateProfileCandidate([FromForm] UpdateProfileCandidateControllerDTO profileCandidate)
        {
            try
            {
                bool isConnect = await _candidatePofileRepository.checkConnection();
                if (!isConnect)
                {
                    Console.WriteLine("Database connection failed");
                    return StatusCode(500, new { message = "Database connection failed" });
                 
                }
                var folderName = _configuration["UploadSettings:AvatarFolder"];
                var uploadsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, folderName);
                Console.WriteLine(uploadsFolder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var extension = Path.GetExtension(profileCandidate.FormFile.FileName); // ".jpg"
                Console.WriteLine(extension);
                var FileName = $"{profileCandidate.accoutnId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, FileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await profileCandidate.FormFile.CopyToAsync(stream);
                }
                var profileCandidateModel = new ProfileCandidateModel()
                {
                    AccountId = profileCandidate.accoutnId,
                    FullName = profileCandidate.fullName,
                    PhoneNumber = profileCandidate.phoneNumber,
                    AvatarURL = Path.Combine(folderName, FileName)
                };
                Console.WriteLine(profileCandidateModel.AvatarURL);
                var result = await _candidatePofileRepository.UpdateProfileCandidate(profileCandidateModel);
                if (!string.IsNullOrEmpty(result.oldAvatarURL))
                {
                    var oldAvatarFullPath = Path.Combine(_webHostEnvironment.ContentRootPath, result.oldAvatarURL);
                    if (System.IO.File.Exists(oldAvatarFullPath))
                    {
                        System.IO.File.Delete(oldAvatarFullPath);
                    }
                }
                if (result != null)
                {
                    return Ok(new { message = "Update profile candidate successful", profileCandidate = result });
                }
                return BadRequest(new { message = "No profile candidate found to update" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in UpdateProfileCandidate for AccountId: {AccountId}", profileCandidate.accoutnId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }


        [HttpGet("getProfileCandidateByAccountId/{accountId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> getProfileCandidateByAccountId(int accountId)
        {
       
            try
            {
                
                bool isConnect = await _candidatePofileRepository.checkConnection();
                if (!isConnect)
                {
                    return StatusCode(500, new { message = "Database connection failed" });
                }
                var result = await _candidatePofileRepository.getProfileCandidateByAccountId(accountId);
              
                result.AvatarURL = $"{Request.Scheme}://{Request.Host}/{result.AvatarURL.Replace("\\", "/")}";
          

                ProfileCandidateResDTO profileCandidateResDTO = new ProfileCandidateResDTO()
                {
                    Email = result.Email,
                    FullName = result.FullName,
                    PhoneNumber = result.PhoneNumber,
                    AvatarURL = result.AvatarURL
                };
                if (result != null)
                {
                    return Ok(new { message = "Get  candidate profile successfully", profile = profileCandidateResDTO });
                }
                return BadRequest(new { message = "No profile candidate found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in getProfileCandidateByAccountId for AccountId: {AccountId}", accountId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }


        [HttpGet("getListCVByAccountID/{accountId}")]
        [ProducesResponseType(typeof(CVsModel), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> getListCVByAccountID(int accountId)
        {
            try
            {
                if (!await _candidatePofileRepository.checkConnection())
                    return StatusCode(500, new { message = "Database connection failed" });

                var result = await _candidatePofileRepository.getListCVByAccountID(accountId);

                if (result != null && result.Count > 0)
                {
                    foreach (var cv in result)
                    {
                        cv.FilePath = $"{Request.Scheme}://{Request.Host}/{cv.FilePath.Replace("\\", "/")}";
                    }
                }

                return Ok(new { message = "Get list CV successfully", CVs = result ?? new List<CVsModel>() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in getListCVByAccountID for AccountId: {AccountId}", accountId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
