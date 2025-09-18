using Capstone.Database;
using Capstone.DTOs.Dashboard;
using Capstone.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Services
{
    public class DashboardAccountServices : IDashboardAccountRepository
    {
        public readonly AppDbContext _context;
        private readonly string _connectionString;
        private readonly ILogger<DashboardAccountServices> _logger;
        public DashboardAccountServices(IConfiguration configuration, AppDbContext context, ILogger<DashboardAccountServices> logger)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _logger = logger;
        }
        public async Task<bool> checkConnection()
        {
            try
            {
                bool isConn = await _context.Database.CanConnectAsync();
                return isConn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                return false;
            }
        }
        public async Task<bool> DeleteAccount(int accountId)
        {
            try
            {
                bool isConnected = await checkConnection();
                if (!isConnected)
                {
                    _logger.LogError("Database connection failed.");
                    return false; // or handle the error as needed
                }
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var authModel = await _context.authModels.Where(a => a.AccountId == accountId).FirstOrDefaultAsync();
                        if (authModel == null)
                        {
                            _logger.LogWarning($"Account with ID {accountId} not found.");
                            return false; // Account not found
                        }
                        if (authModel.Role == "Candidate")
                        {
                            int deletedProfileCount = await _context.profile_CDD_Admins
                                .Where(p => p.AccountId == accountId)
                                .ExecuteDeleteAsync();
                        }
                        if (authModel.Role == "Recruiter")
                        {
                            int deletedProfileCount = await _context.profile_Recruiters
                                .Where(p => p.AccountId == accountId)
                                .ExecuteDeleteAsync();
                        }
                        int deletedAuthCount = await _context.authModels
                            .Where(a => a.AccountId == accountId)
                            .ExecuteDeleteAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation($"Successfully deleted account with ID {accountId}");
                        return true; // Successfully deleted

                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, $"Error deleting account with ID {accountId}");
                        return false; // Error during deletion
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total accounts created");
                return false;
            }
        }
        public async Task<List<DashboardAccountDTO>> GetAllAccounts(int pageNumber, int pageSize)
        {
            try
            {
                bool isConnected = await checkConnection();
                if (!isConnected)
                {
                    _logger.LogError("Database connection failed.");
                    return new List<DashboardAccountDTO>();
                }
                // Cách 1 
                var query = from a in _context.authModels
                            join pca in _context.profile_CDD_Admins on a.AccountId equals pca.AccountId into pcaGroup
                            from pca in pcaGroup.DefaultIfEmpty()
                            join pr in _context.profile_Recruiters on a.AccountId equals pr.AccountId into prGroup
                            from pr in prGroup.DefaultIfEmpty()
                            orderby a.CreateAt descending, a.AccountId descending
                            select new DashboardAccountDTO
                            {
                                Email = a.Email,
                                Role = a.Role,
                                CreatedAt = a.CreateAt,
                                FullName = pca != null ? pca.FullName : pr.FullName
                            };
                var dashboardAccountDTOs = await query
                                           .Skip((pageNumber - 1) * pageSize)
                                           .Take(pageSize)
                                           .ToListAsync();

                // Cách 2
                //var candidates = from a in _context.authModels
                //                 join pca in _context.profile_CDD_Admins on a.AccountId equals pca.AccountId
                //                 select new DashboardAccountDTO
                //                 {
                //                     Email = a.Email,
                //                     Role = a.Role,
                //                     CreatedAt = a.CreatedAt,
                //                     FullName = pca.FullName
                //                 };

                //var recruiters = from a in _context.authModels
                //                 join pr in _context.profile_Recruiters on a.AccountId equals pr.AccountId
                //                 select new DashboardAccountDTO
                //                 {
                //                     Email = a.Email,
                //                     Role = a.Role,
                //                     CreatedAt = a.CreatedAt,
                //                     FullName = pr.FullName
                //                 };

                //// Gộp kết quả
                //var result = candidates.Union(recruiters)
                //                       .OrderByDescending(x => x.CreatedAt)
                //                       .ThenByDescending(x => x.Email) // hoặc AccountId
                //                       .Skip((pageNumber - 1) * pageSize)
                //                       .Take(pageSize)
                //                       .ToList();

                _logger.LogInformation("Successfully retrieved all accounts.");
                return dashboardAccountDTOs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all accounts");
                return new List<DashboardAccountDTO>();
            }
        }
        public async Task<int> GetAccountsCreatedInMonth(int month, int year)
        {
            try
            {
                bool isConnected = await checkConnection();
                if (!isConnected)
                {
                    _logger.LogError("Database connection failed.");
                    return 0; // or handle the error as needed
                }
                int isTotalAccount = await _context.authModels
                    .Where(a => a.CreateAt.Month == month && a.CreateAt.Year == year)
                    .CountAsync();
                return isTotalAccount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total accounts created");
                return 0;
            }
        }
        public async Task<int> GetCandidateAccountsCreatedInMonth(int month, int year)
        {
            try
            {
                bool isConnected = await checkConnection();
                if (!isConnected)
                {
                    _logger.LogError("Database connection failed.");
                    return 0; // or handle the error as needed
                }
                int isTotalAccount = await _context.authModels
                    .Where(a => a.CreateAt.Month == month && a.CreateAt.Year == year && a.Role == "Candidate")
                    .CountAsync();
                return isTotalAccount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total accounts created");
                return 0;
            }
        }

        public async Task<int> GetRecruiterAccountsCreatedInMonth(int month, int year)
        {
            try
            {
                bool isConnected = await checkConnection();
                if (!isConnected)
                {
                    _logger.LogError("Database connection failed.");
                    return 0; // or handle the error as needed
                }
                int isTotalAccount = await _context.authModels
                    .Where(a => a.CreateAt.Month == month && a.CreateAt.Year == year && a.Role == "Recruiter")
                    .CountAsync();
                return isTotalAccount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total accounts created");
                return 0;
            }
        }

        public async Task<int> GetTotalAccountsCreated()
        {
            try
            {
                bool isConnected = await checkConnection();
                if (!isConnected)
                {
                    _logger.LogError("Database connection failed.");
                    return 0; // or handle the error as needed
                }
                int totalAccounts = await _context.authModels.CountAsync();
                return totalAccounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total accounts created");
                return 0;
            }

        }
        public async Task<int> GetTotalCandidateAccountsCreated()
        {
            try
            {
                bool isConnected = await checkConnection();
                if (!isConnected)
                {
                    _logger.LogError("Database connection failed.");
                    return 0; // or handle the error as needed
                }
                int totalAccounts = await _context.authModels.Where(a => a.Role == "Candidate").CountAsync();
                return totalAccounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total accounts created");
                return 0;
            }
        }
        public async Task<int> GetTotalRecruiterAccountsCreated()
        {
            try
            {
                bool isConnected = await checkConnection();
                if (!isConnected)
                {
                    _logger.LogError("Database connection failed.");
                    return 0; // or handle the error as needed
                }
                int totalAccounts = await _context.authModels.Where(a => a.Role == "Recruiter").CountAsync();
                return totalAccounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total accounts created");
                return 0;
            }
        }
    }
}
