using Capstone.DTOs.RecruiterProfile;
using Capstone.Model;
using Capstone.Repositories.Profile;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecruiterProfileController : ControllerBase
    {
        private readonly ILogger<RecruiterProfileController> _logger;
        private readonly IRecruiterProfileRepository _recruiterProfileRepo;

        public RecruiterProfileController(ILogger<RecruiterProfileController> logger, IRecruiterProfileRepository recruiterProfileRepo)
        {
            _logger = logger;
            _recruiterProfileRepo = recruiterProfileRepo;
        }

        [HttpPost("jd")]
        public async Task<IActionResult> CreateJD([FromBody] RecruiterProfileCreateJDDTO createDto)
        {
            try
            {
                if (createDto == null)
                    return BadRequest(new { message = "Request body is required." });

                if (!await _recruiterProfileRepo.checkConnection())
                {
                    _logger.LogError("Database connection failed (CreateJD).");
                    return StatusCode(503, "Database connection failed.");
                }

                var created = await _recruiterProfileRepo.CreateJD(createDto);
                if (created) return Ok(new { message = "JD created successfully" });

                return BadRequest(new { message = "Failed to create JD" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating JD");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("jd")]
        public async Task<IActionResult> UpdateJD([FromBody] RecruiterProfileUpdateJDDTO updateDto)
        {
            try
            {
                if (updateDto == null)
                    return BadRequest(new { message = "Request body is required." });

                if (!await _recruiterProfileRepo.checkConnection())
                {
                    _logger.LogError("Database connection failed (UpdateJD).");
                    return StatusCode(503, "Database connection failed.");
                }

                var updated = await _recruiterProfileRepo.UpdateJD(updateDto);
                if (updated) return Ok(new { message = "JD updated successfully" });

                return NotFound(new { message = "JD not found or update failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating JD with ID {JDId}", updateDto?.JDId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("jd/{JDid:int}")]
        public async Task<IActionResult> DeleteJD([FromRoute] int JDid)
        {
            try
            {
                if (JDid <= 0) return BadRequest(new { message = "Invalid JD id." });

                if (!await _recruiterProfileRepo.checkConnection())
                {
                    _logger.LogError("Database connection failed (DeleteJD).");
                    return StatusCode(503, "Database connection failed.");
                }

                var deleted = await _recruiterProfileRepo.DeleteJD(JDid);
                if (deleted) return Ok(new { message = "JD deleted successfully" });

                return NotFound(new { message = "JD not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting JD with id {Id}", JDid);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("positions")]
        [ProducesResponseType(typeof(List<PositionModel>), 200)]
        [ProducesResponseType(500)]

        public async Task<IActionResult> GetAllPosition()
        {
            try
            {
                if (!await _recruiterProfileRepo.checkConnection())
                {
                    _logger.LogError("Database connection failed (GetAllPosition).");
                    return StatusCode(503, "Database connection failed.");
                }

                var positions = await _recruiterProfileRepo.getAllPosition();
                return Ok(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting positions");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("jds/{accountId:int}")]
        [ProducesResponseType(typeof(List<RecruiterProfileShowJDDTO>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllJD([FromRoute] int accountId)
        {
            try
            {
                if (accountId <= 0) return BadRequest(new { message = "Invalid accountId." });

                if (!await _recruiterProfileRepo.checkConnection())
                {
                    _logger.LogError("Database connection failed (GetAllJD).");
                    return StatusCode(503, "Database connection failed.");
                }

                var jds = await _recruiterProfileRepo.GetAllJD(accountId);
                if (jds == null || jds.Count == 0)
                    return NotFound(new { message = "No JDs found for the given accountId." });
                _logger.LogInformation("Retrieved {Count} JDs for accountId {AccountId}", jds.Count, accountId);
                return Ok(jds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving JDs for accountId {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
