using Capstone.Database;
using Capstone.DTOs.Admin;
using Capstone.Repositories.Admin;
using Google.Apis.Upload;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace Capstone.Services
{
    public class AdminService : IAdminRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AdminService> _logger;
        public AdminService (AppDbContext appDbContext , ILogger<AdminService> logger)
        {
            _dbContext = appDbContext;
            _logger = logger;
        }
        public async Task<bool> BanAccount(int accountId)
        {
            try
            {
                _logger.LogInformation("BanAccount: Starting ban process for AccountId={AccountId}", accountId);

                int affectedRows = await _dbContext.authModels
                    .Where(a => a.AccountId == accountId)
                    .ExecuteUpdateAsync(u => u.SetProperty(a => a.IsActive, a => false));

                if (affectedRows > 0)
                {
                    _logger.LogInformation("BanAccount: Successfully banned AccountId={AccountId}", accountId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("BanAccount: No records were updated for AccountId={AccountId}", accountId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BanAccount: Error while banning AccountId={AccountId}", accountId);
                return false;
            }
        }
        public async Task<bool> UnBanAccount(int accountId)
        {
            try
            {
                _logger.LogInformation("UnBanAccount: Starting unban process for AccountId={AccountId}", accountId);

                int affectedRows = await _dbContext.authModels
                    .Where(a => a.AccountId == accountId)
                    .ExecuteUpdateAsync(u => u.SetProperty(a => a.IsActive, a => true));

                if (affectedRows > 0)
                {
                    _logger.LogInformation("UnBanAccount: Successfully unbanned AccountId={AccountId}", accountId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("UnBanAccount: No records were updated for AccountId={AccountId}", accountId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UnBanAccount: Error while unbanning AccountId={AccountId}", accountId);
                return false;
            }
        }


        public async Task<List<AllAccountByRoleDTO>> GetAllAccountByRole(int page, int pageSize)
        {
            try
            {
                return await _dbContext.authModels
                    .OrderBy(acc => acc.AccountId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(acc => new AllAccountByRoleDTO
                    {
                        AccountId = acc.AccountId,
                        Email = acc.Email,
                        Role = acc.Role,
                        IsActive = acc.IsActive,
                        CreateAt = acc.CreateAt,
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
               
                return new List<AllAccountByRoleDTO>();
            }
        }

        public async Task<int> GetNumberOfCreatedAccount()
        {
            try
            {
                int totalAccount = await _dbContext.authModels.CountAsync();
                return totalAccount;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error getting total Account create");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedAccountByMonth(int month, int year)
        {
            try
            {
                int totalAccountByMonth = await _dbContext.authModels
                    .Where(acc => acc.CreateAt.Month == month && acc.CreateAt.Year == year)
                    .CountAsync();
                return totalAccountByMonth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account create by Month ");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedQuizzes()
        {
            try
            {
                int totalQuizzes = await _dbContext.quizzes.CountAsync(); 
                return totalQuizzes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total Quizzes create ");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedQuizzesByMonth(int month, int year)
        {
            try
            {
                int totalQuizzesByMotnh = await _dbContext.quizzes
                    .Where(quizzes => quizzes.CreateAt.Month == month 
                        && quizzes.CreateAt.Year == year)
                    .CountAsync();
                return totalQuizzesByMotnh;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total Quizzes create by Month ");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedStudentAcount()
        {
            try
            {
                int totalAccountStudent = await _dbContext.authModels
                    .Where(accStudent => accStudent.Role == "Student")
                    .CountAsync();
                return totalAccountStudent; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account Student create");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedStudentAcountByMonth(int month, int year) 
        {
            try
            {
                int totalAccountStudentByMonth = await _dbContext.authModels
                    .Where(accStudent => accStudent.Role == "Student" 
                        && accStudent.CreateAt.Month == month 
                        && accStudent.CreateAt.Year == year)
                    .CountAsync();
                return totalAccountStudentByMonth;
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account Student create by Month ");
                return 0;
            }
        }

        public async Task<int> GetNumberOfCreatedTeacherAccount()
        {
            try
            {
                int totalAccountTeacher = await _dbContext.authModels.Where(accTeacher => accTeacher.Role == "Teacher").CountAsync();
                return totalAccountTeacher;
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account Teacher create");
                return 0;
            }
            
        }

        public async Task<int> GetNumberOfCreatedTeacherAccountByMonth(int month, int year)
        {
            try
            {
                int toltalAccountTeacherByMonth = await _dbContext.authModels
                    .Where(accTeacher => accTeacher.Role == "Teacher"
                        && accTeacher.CreateAt.Month == month
                        && accTeacher.CreateAt.Year == year)
                    .CountAsync();
                return toltalAccountTeacherByMonth;                
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting total Account Teacher create by Month ");
                return 0;
            }
        }
    }
}
