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
        private readonly Redis _redis;
        public AdminController(IAdminRepository adminRepository, ILogger<AdminController> logger, Redis redis)
        {
            _adminRepository = adminRepository;
            _logger = logger;
            _redis = redis;
        }

        [HttpDelete("deleteAccount/{accountId}")]
        public async Task<IActionResult> DeleteAccount(int accountId)
        {
            try
            {
                bool isDeleted = await _adminRepository.DeleteAccount(accountId);

                if (!isDeleted)
                {
                    return NotFound(new
                    {
                        message = $"Account with ID {accountId} not found or could not be deleted."
                    });
                }

                return Ok(new
                {
                    message = $"Account with ID {accountId} deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account {accountId}", accountId);
                return StatusCode(500, new
                {
                    message = "Internal server error while deleting account.",
                    detail = ex.Message
                });
            }
        }

        [HttpGet("getAllAccountsByRole")]
        public async Task<IActionResult> GetAllAccountsByRole([FromQuery] PaginationDTO pages, [FromQuery] string role)
        {
            if (pages.page <= 0 || pages.pageSize <= 0)
                return BadRequest(new { message = "Page and PageSize must be greater than 0." });

            if (string.IsNullOrEmpty(role))
                return BadRequest(new { message = "Role is required." });

            // tạo cache key cho role + phân trang
            string cacheKey = $"accounts_{role}_page_{pages.page}_size_{pages.pageSize}";
            var cachedJson = await _redis.GetStringAsync(cacheKey);

            List<AllAccountByRoleDTO>? accounts = null;

            if (!string.IsNullOrEmpty(cachedJson))
            {
                accounts = JsonSerializer.Deserialize<List<AllAccountByRoleDTO>>(cachedJson);
            }

            if (accounts == null || !accounts.Any())
            {
                accounts = await _adminRepository.GetAllAccountByRole(pages.page, pages.pageSize, role);

                if (accounts == null || !accounts.Any())
                    return NotFound(new { message = $"No accounts found for role {role}." });

                // luu cache 5 phút
                await _redis.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(accounts),
                    TimeSpan.FromMinutes(5)
                );
            }

            return Ok(accounts);
        }

        [HttpGet("getAccount")]
        public async Task<IActionResult> GetNumberOfCreatedAccount()
        {
            var total = await _adminRepository.GetNumberOfCreatedAccount();
            return Ok(total);
        }

        [HttpGet("getAccountByMonth/{month}/{year}")]
        public async Task<IActionResult> GetNumberOfCreatedAccountByMonth(int month, int year)
        {
            var total = await _adminRepository.GetNumberOfCreatedAccountByMonth(month, year);
            return Ok(total);
        }

        [HttpGet("getQuizzes")]
        public async Task<IActionResult> GetNumberOfCreatedQuizzes()
        {
            var total = await _adminRepository.GetNumberOfCreatedQuizzes();
            return Ok(total);
        }

        [HttpGet("getQuizzesByMonth/{month}/{year}")]
        public async Task<IActionResult> GetNumberOfCreatedQuizzesByMonth(int month, int year)
        {
            var total = await _adminRepository.GetNumberOfCreatedQuizzesByMonth(month , year);
            return Ok(total);
        }

        [HttpGet("getAccountStudent")]
        public async Task<IActionResult> GetNumberOfCreatedStudentAcount()
        {
            var total = await _adminRepository.GetNumberOfCreatedStudentAcount();
            return Ok(total);
        }

        [HttpGet("getAccountStudentByMonth/{month}/{year}")]
        public async Task<IActionResult> GetNumberOfCreatedStudentAcountByMonth(int month, int year)
        {
            var total = await _adminRepository.GetNumberOfCreatedStudentAcountByMonth(month, year);
            return Ok(total);
        }

        [HttpGet("getAccountTeacher")]
        public async Task<IActionResult> GetNumberOfCreatedTeacherAccount()
        {
            var total = await _adminRepository.GetNumberOfCreatedTeacherAccount();
            return Ok(total);
        }

        [HttpGet("getAccountTeacherByMonth/{month}/{year}")]
        public async Task<IActionResult> GetNumberOfCreatedTeacherAccountByMonth(int month, int year)
        {
            var total = await _adminRepository.GetNumberOfCreatedTeacherAccountByMonth(month, year);
            return Ok(total);
        }

    }
}
