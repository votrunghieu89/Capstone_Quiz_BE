using Capstone.DTOs;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
using Capstone.Repositories;
using Capstone.Repositories.Filter_Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchRepository _repo;
        private readonly ILogger<SearchController> _logger;
        private readonly IAWS _S3;
        public SearchController(ISearchRepository repo, ILogger<SearchController> logger, IAWS S3)
        {
            _repo = repo;
            _logger = logger;
            _S3 = S3;
        }

        [HttpGet("filterByRole")]
        public async Task<IActionResult> FilterByRole(string role, [FromQuery] PaginationDTO pages)
        {
            try
            {
                if (pages.page <= 0 || pages.pageSize <= 0)
                {
                    _logger.LogWarning("Invalid pagination parameters: Page={Page}, PageSize={PageSize}",
                        pages.page, pages.pageSize);
                    return BadRequest(new { message = "Page and PageSize must be greater than 0." });
                }

                var accountListByRole =await _repo.FilterByRole(role, pages.page, pages.pageSize);

                _logger.LogInformation("Retrieved accounts list: Page={Page}, PageSize={PageSize}, Count={Count}",
                     pages.page, pages.pageSize, accountListByRole);

                return Ok(accountListByRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accounts list: Page={Page}, PageSize={PageSize}",
                                 pages.page, pages.pageSize);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("filterByTopic")]
        public async Task<IActionResult> FilterByTopic(int topic, [FromQuery] PaginationDTO pages)
        {
            try
            {
                if (pages.page <= 0 || pages.pageSize <= 0)
                {
                    _logger.LogWarning("Invalid pagination parameters: Page={Page}, PageSize={PageSize}",
                        pages.page, pages.pageSize);
                    return BadRequest(new { message = "Page and PageSize must be greater than 0." });
                }

                var topicList =await _repo.FilterByTopic(topic, pages.page, pages.pageSize);
                _logger.LogInformation("Retrieved topic list: Page={Page}, PageSize={PageSize}, Count={Count}",
                     pages.page, pages.pageSize, topicList);
                return Ok(topicList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving topic list: Page={Page}, PageSize={PageSize}",
                                 pages.page, pages.pageSize);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("searchAccountByEmail")]
        public async Task<IActionResult> SearchAccountByEmail(string email)
        {
            try
            {
                var acc=await _repo.SearchAccountByEmail(email);
                _logger.LogInformation($"Search account by email: {email}");
                if(acc == null)
                {
                    return NotFound(new { message = "Account not found with the provided email." });
                }
                return Ok(acc);
            }catch (Exception ex)
            {
                _logger.LogError(ex, "Error search account by email");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("searchParticipantInGroup")]
        public async Task<IActionResult> SearchParticipantInGroup(string Name, int groupId)
        {
            try
            {
                var parListGr = await _repo.SearchParticipantInGroup(Name, groupId);
                foreach (var par in parListGr)
                {
                    if (par.Avatar != null)
                    {
                        par.Avatar = await _S3.ReadImage(par.Avatar);
                    }
                }
                _logger.LogInformation($"Search Participant In Group by Name and groupId: {Name}, {groupId}");

                if (parListGr == null || !parListGr.Any())
                {
                    return NotFound(new { message = "No participants found in this group with the given name." });
                }

                return Ok(parListGr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Participant In Group by Name and groupId");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("searchStudentInOfflineReport")]
        public async Task<IActionResult> SearchStudentInOfflineReport(string Name, int reportId)
        {
            try
            {
                var studentOfReportList = await _repo.SearchStudentInOfflineReport(Name, reportId);
                _logger.LogInformation($"Search Student In Offline Report by Name and reportId: {Name}, {reportId}");

                if (studentOfReportList == null || !studentOfReportList.Any())
                {
                    return NotFound(new { message = "No students found in this offline report with the given name." });
                }

                return Ok(studentOfReportList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Student In Offline Report by Name and reportId");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("searchStudentInOnlineReport")]
        public async Task<IActionResult> SearchStudentInOnlineReport(string Name, int reportId)
        {
            try
            {
                var studentOnlReportList = await _repo.SearchStudentInOnlineReport(Name, reportId);
                _logger.LogInformation($"Search Student In Online Report by Name and reportId: {Name}, {reportId}");

                if (studentOnlReportList == null || !studentOnlReportList.Any())
                {
                    return NotFound(new { message = "No students found in this online report with the given name." });
                }

                return Ok(studentOnlReportList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Student In Online Report by Name and reportId");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
