using Capstone.DTOs.Dashboard;
using Capstone.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardAccountController : ControllerBase
    {
        private readonly IDashboardAccountRepository _dashboardService;
        private readonly ILogger<DashboardAccountController> _logger;

        public DashboardAccountController(IDashboardAccountRepository dashboardService, ILogger<DashboardAccountController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // GET: api/DashboardAccount?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetAllAccounts(int pageNumber , int pageSize )
        {
            var result = await _dashboardService.GetAllAccounts(pageNumber, pageSize);
            return Ok(result);
        }

        // DELETE: api/DashboardAccount/5
        [HttpDelete("{accountId}")]
        public async Task<IActionResult> DeleteAccount(int accountId)
        {
            var isDeleted = await _dashboardService.DeleteAccount(accountId);
            if (!isDeleted)
                return NotFound(new { message = $"Account with ID {accountId} not found" });

            return Ok(new { message = "Account deleted successfully" });
        }

        // GET: api/DashboardAccount/total
        [HttpGet("total")]
        public async Task<IActionResult> GetTotalAccounts()
        {
            var total = await _dashboardService.GetTotalAccountsCreated();
            return Ok(total);
        }

        // GET: api/DashboardAccount/total/candidates
        [HttpGet("total/candidates")]
        public async Task<IActionResult> GetTotalCandidateAccounts()
        {
            var total = await _dashboardService.GetTotalCandidateAccountsCreated();
            return Ok(total);
        }

        // GET: api/DashboardAccount/total/recruiters
        [HttpGet("total/recruiters")]
        public async Task<IActionResult> GetTotalRecruiterAccounts()
        {
            var total = await _dashboardService.GetTotalRecruiterAccountsCreated();
            return Ok(total);
        }

        // GET: api/DashboardAccount/byMonth?month=9&year=2025
        [HttpGet("byMonth")]
        public async Task<IActionResult> GetAccountsCreatedInMonth(int month, int year)
        {
            var total = await _dashboardService.GetAccountsCreatedInMonth(month, year);
            return Ok(total);
        }

        // GET: api/DashboardAccount/candidates/byMonth?month=9&year=2025
        [HttpGet("candidates/byMonth")]
        public async Task<IActionResult> GetCandidateAccountsCreatedInMonth(int month, int year)
        {
            var total = await _dashboardService.GetCandidateAccountsCreatedInMonth(month, year);
            return Ok(total);
        }

        // GET: api/DashboardAccount/recruiters/byMonth?month=9&year=2025
        [HttpGet("recruiters/byMonth")]
        public async Task<IActionResult> GetRecruiterAccountsCreatedInMonth(int month, int year)
        {
            var total = await _dashboardService.GetRecruiterAccountsCreatedInMonth(month, year);
            return Ok(total);
        }
    }
}
