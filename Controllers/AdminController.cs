using Capstone.Database;
using Capstone.DTOs;
using Capstone.DTOs.Admin;
using Capstone.Repositories.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _adminRepository;
        private readonly ILogger<AdminController> _logger;
        private readonly IRedis _redis;
        
        public AdminController(IAdminRepository adminRepository, ILogger<AdminController> logger, IRedis redis)
        {
            _adminRepository = adminRepository;
            _logger = logger;
            _redis = redis;
        }

        // ===== GET METHODS =====
        [HttpGet("getAllAccounts")]
        public async Task<IActionResult> GetAllAccountsByRole([FromQuery] PaginationDTO pages)
        {
            try
            {
                if (pages.page <= 0 || pages.pageSize <= 0)
                {
                    _logger.LogWarning("Invalid pagination parameters: Page={Page}, PageSize={PageSize}", 
                        pages.page, pages.pageSize);
                    return BadRequest(new { message = "Page and PageSize must be greater than 0." });
                }

                var accountList = await _adminRepository.GetAllAccountByRole(pages.page, pages.pageSize);
                
                _logger.LogInformation("Retrieved accounts list: Page={Page}, PageSize={PageSize}, Count={Count}", 
                    pages.page, pages.pageSize, accountList?.Count ?? 0);
                
                return Ok(accountList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accounts list: Page={Page}, PageSize={PageSize}", 
                    pages.page, pages.pageSize);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getAccount")]
        public async Task<IActionResult> GetNumberOfCreatedAccount()
        {
            try
            {
                var total = await _adminRepository.GetNumberOfCreatedAccount();
                _logger.LogInformation("Retrieved total account count: {Total}", total);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total account count");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getAccountByMonth/{month}/{year}")]
        public async Task<IActionResult> GetNumberOfCreatedAccountByMonth(int month, int year)
        {
            try
            {
                if (month < 1 || month > 12)
                {
                    _logger.LogWarning("Invalid month parameter: Month={Month}", month);
                    return BadRequest(new { message = "Month must be between 1 and 12" });
                }

                var total = await _adminRepository.GetNumberOfCreatedAccountByMonth(month, year);
                _logger.LogInformation("Retrieved account count by month: Month={Month}, Year={Year}, Total={Total}", 
                    month, year, total);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account count by month: Month={Month}, Year={Year}", 
                    month, year);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getQuizzes")]
        public async Task<IActionResult> GetNumberOfCreatedQuizzes()
        {
            try
            {
                var total = await _adminRepository.GetNumberOfCreatedQuizzes();
                _logger.LogInformation("Retrieved total quizzes count: {Total}", total);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total quizzes count");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getQuizzesByMonth/{month}/{year}")]
        public async Task<IActionResult> GetNumberOfCreatedQuizzesByMonth(int month, int year)
        {
            try
            {
                if (month < 1 || month > 12)
                {
                    _logger.LogWarning("Invalid month parameter: Month={Month}", month);
                    return BadRequest(new { message = "Month must be between 1 and 12" });
                }

                var total = await _adminRepository.GetNumberOfCreatedQuizzesByMonth(month, year);
                _logger.LogInformation("Retrieved quizzes count by month: Month={Month}, Year={Year}, Total={Total}", 
                    month, year, total);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quizzes count by month: Month={Month}, Year={Year}", 
                    month, year);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getAccountStudent")]
        public async Task<IActionResult> GetNumberOfCreatedStudentAcount()
        {
            try
            {
                var total = await _adminRepository.GetNumberOfCreatedStudentAcount();
                _logger.LogInformation("Retrieved total student account count: {Total}", total);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total student account count");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getAccountStudentByMonth/{month}/{year}")]
        public async Task<IActionResult> GetNumberOfCreatedStudentAcountByMonth(int month, int year)
        {
            try
            {
                if (month < 1 || month > 12)
                {
                    _logger.LogWarning("Invalid month parameter: Month={Month}", month);
                    return BadRequest(new { message = "Month must be between 1 and 12" });
                }

                var total = await _adminRepository.GetNumberOfCreatedStudentAcountByMonth(month, year);
                _logger.LogInformation("Retrieved student account count by month: Month={Month}, Year={Year}, Total={Total}", 
                    month, year, total);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student account count by month: Month={Month}, Year={Year}", 
                    month, year);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getAccountTeacher")]
        public async Task<IActionResult> GetNumberOfCreatedTeacherAccount()
        {
            try
            {
                var total = await _adminRepository.GetNumberOfCreatedTeacherAccount();
                _logger.LogInformation("Retrieved total teacher account count: {Total}", total);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total teacher account count");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getAccountTeacherByMonth/{month}/{year}")]
        public async Task<IActionResult> GetNumberOfCreatedTeacherAccountByMonth(int month, int year)
        {
            try
            {
                if (month < 1 || month > 12)
                {
                    _logger.LogWarning("Invalid month parameter: Month={Month}", month);
                    return BadRequest(new { message = "Month must be between 1 and 12" });
                }

                var total = await _adminRepository.GetNumberOfCreatedTeacherAccountByMonth(month, year);
                _logger.LogInformation("Retrieved teacher account count by month: Month={Month}, Year={Year}, Total={Total}", 
                    month, year, total);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving teacher account count by month: Month={Month}, Year={Year}", 
                    month, year);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ===== PUT METHODS =====

        /// <summary>
        /// Ban an account by setting IsActive to false
        /// </summary>
        [HttpPut("ban-account/{accountId:int}")]
        public async Task<IActionResult> BanAccount(int accountId)
        {
            try
            {
                if (accountId <= 0)
                {
                    _logger.LogWarning("Invalid accountId provided for ban: AccountId={AccountId}", accountId);
                    return BadRequest(new { message = "Invalid account ID" });
                }

                var success = await _adminRepository.BanAccount(accountId);

                if (success)
                {
                    _logger.LogInformation("Successfully banned account: AccountId={AccountId}", accountId);
                    return Ok(new { message = "Account banned successfully", accountId });
                }
                else
                {
                    _logger.LogWarning("Failed to ban account - account not found: AccountId={AccountId}", accountId);
                    return NotFound(new { message = "Account not found or already banned" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning account: AccountId={AccountId}", accountId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Unban an account by setting IsActive to true
        /// </summary>
        [HttpPut("unban-account/{accountId:int}")]
        public async Task<IActionResult> UnBanAccount(int accountId)
        {
            try
            {
                if (accountId <= 0)
                {
                    _logger.LogWarning("Invalid accountId provided for unban: AccountId={AccountId}", accountId);
                    return BadRequest(new { message = "Invalid account ID" });
                }

                var success = await _adminRepository.UnBanAccount(accountId);

                if (success)
                {
                    _logger.LogInformation("Successfully unbanned account: AccountId={AccountId}", accountId);
                    return Ok(new { message = "Account unbanned successfully", accountId });
                }
                else
                {
                    _logger.LogWarning("Failed to unban account - account not found: AccountId={AccountId}", accountId);
                    return NotFound(new { message = "Account not found or already active" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning account: AccountId={AccountId}", accountId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
