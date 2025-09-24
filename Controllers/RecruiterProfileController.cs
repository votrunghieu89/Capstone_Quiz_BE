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
        public RecruiterProfileController(IRecruiterProfileRepository service)
        {
            _service = service;
        }
        //[Authorize("Recruiter")]
        [HttpPost]
        public async Task<IActionResult> CreateJDs(RecruiterProfileCreateJDDTO recruterProfileCreateJDDTO)
        {
            var result = await _service.CreateJD(recruterProfileCreateJDDTO);
            if(!result)
            {
                return BadRequest(result);
            }
            return Ok(result); 

        }

        [HttpDelete]
        public async Task<IActionResult> DeleteJD(int JDid)
        {
            var result = await _service.DeleteJD(JDid);
            if (!result)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateJD(RecruiterProfileUpdateJDDTO recruterProfileUpdateJDDTO)
        {
            var result = await _service.UpdateJD(recruterProfileUpdateJDDTO);
            if (!result)
            {
                return BadRequest(result);
            }
            return(Ok(result));
        }
    }
}
