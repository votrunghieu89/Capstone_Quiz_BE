using Capstone.DTOs;
using Capstone.DTOs.Reports.Teacher.OnlineReport;
using Capstone.Repositories.Filter_Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchRepository _repo;
        private readonly ILogger<SearchController> _logger;
        public SearchController(ISearchRepository repo, ILogger<SearchController> logger)
        {
            _repo = repo;
            _logger = logger;
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
                var acc =await _repo.SearchAccountByEmail(email);
                _logger.LogInformation($"Search account by email: {email}");
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
                _logger.LogInformation($"Search Participant In Group by Name and groupId :{Name} {groupId} ");
                return Ok(parListGr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error search Participant In Group by Name and groupId");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("searchStudentInOfflineReport")]
        public async Task<IActionResult> SearchStudentInOfflineReport(string Name, int reportId)
        {
            try
            {
                var studentOfReportList =await _repo.SearchStudentInOfflineReport(Name, reportId);
                _logger.LogInformation($"Search Student In Offline Report by Name and reportId :{Name} {reportId} ");
                return Ok(studentOfReportList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error search Student In Offline Report by Name and reportId");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("searchStudentInOnlineReport")]
        public async Task<IActionResult> SearchStudentInOnlineReport(string Name, int reportId)
        {
            try
            {
                var studentOnlReportList = await _repo.SearchStudentInOnlineReport(Name, reportId);
                _logger.LogInformation($"Search Student In Online Report by Name and reportId :{Name} {reportId} ");
                return Ok(studentOnlReportList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error search Student In Online Report by Name and reportId");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
