using Capstone.DTOs.RecruiterProfile;
using Capstone.Repositories.Profile;
using Capstone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecruiterProfileController : ControllerBase
    {
        public readonly IRecruiterProfileRepository _service;
        public readonly ILogger<RecruiterProfileController> _logger;

        public RecruiterProfileController(IRecruiterProfileRepository service, ILogger<RecruiterProfileController> logger)
        {
            _service = service;
            _logger = logger;
        }

        //[Authorize("Recruiter")]
        [HttpPost("CreateJd")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateJDs(RecruiterProfileCreateJDDTO recruterProfileCreateJDDTO)
        {
        
            try
            {
                var result = await _service.CreateJD(recruterProfileCreateJDDTO);
                if (!result)
                {
                    _logger.LogWarning("CreateJD failed for data: {@DTO}", recruterProfileCreateJDDTO);
                    return BadRequest(new { message = "Create Fail" });
                }

                _logger.LogInformation("CreateJD success for data: {@DTO}", recruterProfileCreateJDDTO);
                return Ok(new { message = "Create Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in CreateJD for data: {@DTO}", recruterProfileCreateJDDTO);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("DeleteJd")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteJD(int JDid)
        {
           
            try
            {
                var result = await _service.DeleteJD(JDid);
                if (!result)
                {
                    _logger.LogWarning("DeleteJD failed for Id: {JDid}", JDid);
                    return BadRequest(new { message = "Delete Fail" });
                }

                _logger.LogInformation("DeleteJD success for Id: {JDid}", JDid);
                return Ok(new { message = "Delete Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in DeleteJD for Id: {JDid}", JDid);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("UpdateJD")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateJD(RecruiterProfileUpdateJDDTO recruterProfileUpdateJDDTO)
        {
          
            try
            {
                var result = await _service.UpdateJD(recruterProfileUpdateJDDTO);
                if (!result)
                {
                    _logger.LogWarning("UpdateJD failed for data: {@DTO}", recruterProfileUpdateJDDTO);
                    return BadRequest(new { message = "Update Fail" });
                }

                _logger.LogInformation("UpdateJD success for data: {@DTO}", recruterProfileUpdateJDDTO);
                return Ok(new { message = "Update Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in UpdateJD for data: {@DTO}", recruterProfileUpdateJDDTO);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
